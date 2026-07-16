using System;
using UJam.Runtime.Grid;
using UnityEngine;

namespace UJam.Runtime.Navigation
{
    public sealed class NavigationDriver : MonoBehaviour, EnemyNavigationPort
    {
        // 경로 Provider Component 연결 슬롯
        [SerializeField]
        private MonoBehaviour _pathfinderComponent;

        // 실제 이동 Motor Component 연결 슬롯
        [SerializeField]
        private MonoBehaviour _movementMotorComponent;

        // 통과 능력 Provider Component 연결 슬롯
        [SerializeField]
        private MonoBehaviour _traversalComponent;

        // Grid 좌표와 version을 읽는 계약 저장
        private IGridMetrics _gridMetrics;
        // Cell 통과 가능성을 읽는 계약 저장
        private IGridNavigation _gridNavigation;
        // 경로 계산 Provider 계약 저장
        private INavigationPathfinder _pathfinder;
        // 실제 이동 Provider 계약 저장
        private IMovementMotor _movementMotor;
        // 통과 판정 Provider 계약 저장
        private ITraversalCapability _traversalCapability;
        // 현재 이동 요청 저장
        private NavigationRequest _currentRequest;
        // 현재 외부에 공개할 결과 저장
        private NavigationResult _currentResult = NavigationResult.Failed(NavigationFailureReason.GridNotInitialized);
        // 보존된 요청이 있는지 저장
        private bool _hasRequest;
        // 다음 Tick에 한 번 재탐색할지 저장
        private bool _repathRequested;
        // 마지막으로 확인한 Grid version 저장
        private int _observedGridVersion;
        // Grid version 변경을 현재 구독 중인지 저장
        private bool _gridVersionSubscribed;

        // Unity 활성화 시 Inspector Provider를 인터페이스로 변환
        private void Awake()
        {
            // 명시된 Component만 Provider 계약으로 변환
            ResolveSerializedProviders();
        }

        // Unity Update를 승인된 Tick 경계로 전달
        private void Update()
        {
            // Unity 시간만 Core Tick에 전달
            Tick(Time.deltaTime);
        }

        // 활성화 시 이미 주입된 Grid version 구독만 복구
        private void OnEnable()
        {
            // 요청과 Motor 이동을 자동 재개하지 않고 구독만 복구
            SubscribeGridVersion();
        }

        // Grid 계약을 명시적으로 주입하고 version 변경을 구독
        public void Initialize(IGridMetrics gridMetrics, IGridNavigation gridNavigation)
        {
            // 이전 Grid version 구독 해제
            UnsubscribeGridVersion();

            _gridMetrics = gridMetrics;
            _gridNavigation = gridNavigation;

            // 두 Grid 계약이 준비된 경우 version 구독
            SubscribeGridVersion();
        }

        // 외부에서 관찰 가능한 Navigation 상태를 한 Tick 진행
        public void Tick(float deltaTime)
        {
            // 보존된 요청이 없으면 Motor를 호출하지 않음
            if (!_hasRequest)
            {
                // 요청 없이 Tick 종료
                return;
            }

            // terminal 결과는 새 요청 전까지 유지
            if (IsTerminalResult())
            {
                // terminal 결과 유지
                return;
            }

            // 이전 Tick에서 예약된 재탐색을 한 번 처리
            if (_repathRequested)
            {
                // 재탐색 예약을 현재 처리로 소비
                _repathRequested = false;

                // 이전 이동을 먼저 중단하고 실패하면 새 경로를 요청하지 않음
                if (!TryStopMotor())
                {
                    // 재탐색 중단 실패 종료
                    return;
                }

                // 보존된 요청으로 한 번만 경로 재요청
                TryProcessRequest();
                // 재탐색 Tick 종료
                return;
            }

            // Motor가 없어 실제 이동을 진행할 수 없는 상태
            if (_movementMotor == null)
            {
                // Motor 누락을 승인된 실패 결과로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.MotorMissing);
                // Motor 누락 Tick 종료
                return;
            }

            // Motor Tick 결과를 받을 값
            NavigationMotorResult motorResult;

            // Motor Provider 예외를 승인된 실패 결과로 변환
            try
            {
                // 실제 이동 Provider에 현재 Tick 전달
                motorResult = _movementMotor.Tick(deltaTime);
            }
            // Provider 예외 경계
            catch (Exception)
            {
                // 예외를 외부 Navigation 상태로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // Motor Tick 예외 종료
                return;
            }

            // 초기화되지 않은 Motor 결과를 Provider 실패로 변환
            if (!motorResult.IsValid)
            {
                // 잘못된 Motor 결과 저장
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // 잘못된 Motor 결과 Tick 종료
                return;
            }

            // Motor 결과를 Navigation 결과로 변환
            ApplyMotorResult(motorResult);
        }

        // 새 이동 요청을 기존 이동과 교체
        public void RequestNavigation(NavigationRequest request)
        {
            // 기존 Motor 이동을 새 요청 전에 중단
            if (!TryStopMotor())
            {
                // 기존 이동 중단 실패 종료
                return;
            }

            _currentRequest = request;
            _hasRequest = true;
            _repathRequested = false;

            // 새 요청 처리 전에 초기 실패 상태를 준비
            _currentResult = NavigationResult.Failed(NavigationFailureReason.GridNotInitialized);
            // 새 요청의 첫 경로를 즉시 계산
            TryProcessRequest();
        }

        // 현재 Navigation 결과 반환
        public NavigationResult GetCurrentResult()
        {
            // 저장된 상태와 payload 반환
            return _currentResult;
        }

        // 비활성화 시 실제 이동과 활성 요청 중단
        private void OnDisable()
        {
            // 비활성화 시 Motor의 실제 이동 중단
            TryStopMotor();
            // 다시 활성화해도 이전 요청을 자동 재개하지 않음
            _hasRequest = false;
            // 예약된 재탐색 제거
            _repathRequested = false;
            // Grid version 구독 해제
            UnsubscribeGridVersion();
        }

        // 파괴 시 남은 구독과 Motor 작업 정리
        private void OnDestroy()
        {
            // 파괴되는 Driver의 Grid 구독 해제
            UnsubscribeGridVersion();
            // 남은 Motor 작업 정리
            TryStopMotor();
        }

        // SerializeField Component를 승인된 Provider interface로 변환
        private void ResolveSerializedProviders()
        {
            // 자동 탐색 없이 지정된 Pathfinder Component만 변환
            _pathfinder = _pathfinderComponent as INavigationPathfinder;
            // 자동 탐색 없이 지정된 Motor Component만 변환
            _movementMotor = _movementMotorComponent as IMovementMotor;
            // 자동 탐색 없이 지정된 Traversal Component만 변환
            _traversalCapability = _traversalComponent as ITraversalCapability;
        }

        // 보존된 요청을 Grid와 Provider에 전달해 경로 시작
        private void TryProcessRequest()
        {
            // 요청이 없는 내부 호출을 차단
            if (!_hasRequest)
            {
                // 내부 요청 없음 종료
                return;
            }

            // Grid 계약이 모두 준비되지 않은 상태
            if (!IsGridInitialized())
            {
                // Grid 미초기화를 승인된 실패 결과로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.GridNotInitialized);
                // Grid 미초기화 처리 종료
                return;
            }

            // Motor Provider 누락 상태
            if (_movementMotor == null)
            {
                // Motor 누락을 승인된 실패 결과로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.MotorMissing);
                // Motor 누락 처리 종료
                return;
            }

            // 경로 또는 Traversal Provider 누락 상태
            if (_pathfinder == null || _traversalCapability == null)
            {
                // Provider 누락을 승인된 실패 결과로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // Provider 누락 처리 종료
                return;
            }

            // 경로 계산에 사용할 Motor 현재 Cell
            GridCell currentCell;

            // Motor 현재 Cell 조회 예외를 Provider 실패로 변환
            try
            {
                // 실제 이동 Provider에서 현재 Cell 확보
                currentCell = _movementMotor.CurrentCell;
            }
            // Provider 예외 경계
            catch (Exception)
            {
                // 현재 위치 조회 실패를 외부 상태로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // 현재 Cell 조회 처리 종료
                return;
            }

            // Pathfinder에 전달할 좁은 경로 요청
            NavigationPathRequest pathRequest = new NavigationPathRequest(
                currentCell,
                _currentRequest,
                _gridNavigation,
                _traversalCapability);
            // Pathfinder가 반환할 경로 결과
            NavigationPathResult pathResult;

            // Pathfinder Provider 예외를 승인된 실패 결과로 변환
            try
            {
                // 외부 경로 Provider에 좁은 요청 전달
                pathResult = _pathfinder.FindPath(pathRequest);
            }
            // Provider 예외 경계
            catch (Exception)
            {
                // 경로 계산 예외를 외부 상태로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // 경로 Provider 예외 처리 종료
                return;
            }

            // 경로 결과와 Motor 시작을 상태로 반영
            ApplyPathResult(currentCell, pathResult);
        }

        // Pathfinder 결과를 Navigation 상태와 실제 이동으로 변환
        private void ApplyPathResult(GridCell currentCell, NavigationPathResult pathResult)
        {
            // 성공 경로 결과 처리
            if (pathResult.IsSuccess)
            {
                // Provider가 반환한 불변 경로 확보
                NavigationPath path = pathResult.Path;

                // 성공 경로 Cell 목록이 없는 잘못된 Provider 결과 차단
                if (path.Cells == null)
                {
                    // 잘못된 성공 payload를 Provider 실패로 변환
                    _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                    // 잘못된 성공 경로 처리 종료
                    return;
                }

                // 빈 Path의 현재 위치 도착 여부 판정
                if (path.Cells.Count == 0)
                {
                    // 현재 Cell이 목적지인 경우 도착 처리
                    if (currentCell == path.Destination)
                    {
                        // 빈 경로 도착 결과 저장
                        _currentResult = NavigationResult.Arrived();
                        // 현재 Cell 도착 처리 종료
                        return;
                    }

                    // 목적지 아닌 빈 경로를 Provider 실패로 변환
                    _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                    // 목적지 아닌 빈 경로 처리 종료
                    return;
                }

                // 성공 경로를 Motor에 전달하고 실제 이동 시작
                try
                {
                    // 위치 변경 책임을 Motor에 위임
                    _movementMotor.BeginPath(path);
                }
                // Provider 예외 경계
                catch (Exception)
                {
                    // 이동 시작 예외를 외부 상태로 변환
                    _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                    // Motor 시작 예외 처리 종료
                    return;
                }

                // 경로 시작 결과를 이동 중으로 공개
                _currentResult = NavigationResult.Moving();
                // 정상 경로 시작 처리 종료
                return;
            }

            // 유효한 차단 결과 처리
            if (pathResult.IsBlocked)
            {
                // 차단 payload가 유효한 경우에만 공통 Blocked 공개
                if (pathResult.BlockedBy.IsValid)
                {
                    // 구체 장애물 없이 공통 차단 결과 저장
                    _currentResult = NavigationResult.Blocked(
                        pathResult.BlockedBy,
                        pathResult.AttackPosition);
                    // 유효한 차단 처리 종료
                    return;
                }

                // 잘못된 차단 payload를 Provider 실패로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // 잘못된 차단 처리 종료
                return;
            }

            // 승인된 경로 실패 결과 처리
            if (pathResult.IsFailed)
            {
                // NoPath 등 Provider 실패 사유 공개
                _currentResult = NavigationResult.Failed(pathResult.FailureReason);
                // 승인된 경로 실패 처리 종료
                return;
            }

            // 상태가 없는 잘못된 Provider 결과 차단
            _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
        }

        // Motor 결과를 외부 Navigation 상태로 변환
        private void ApplyMotorResult(NavigationMotorResult motorResult)
        {
            // Motor가 반환한 상태 종류 분기
            switch (motorResult.Status)
            {
                // 이동 중 상태 유지
                case NavigationStatus.Moving:
                    // Motor 이동 중 결과 저장
                    _currentResult = NavigationResult.Moving();
                    break;

                // 도착 terminal 상태 유지
                case NavigationStatus.Arrived:
                    // Motor 도착 결과 저장
                    _currentResult = NavigationResult.Arrived();
                    break;

                // 차단 payload 검증 후 terminal 상태 저장
                case NavigationStatus.Blocked:
                    // 유효한 장애물 Handle만 차단 결과로 공개
                    if (motorResult.BlockedBy.IsValid)
                    {
                        // Motor 차단 결과 저장
                        _currentResult = NavigationResult.Blocked(
                            motorResult.BlockedBy,
                            motorResult.AttackPosition);
                    }
                    // 차단 payload가 없을 때 실패 처리
                    else
                    {
                        // 잘못된 차단 payload를 Provider 실패로 변환
                        _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                    }

                    break;

                // 실패 terminal 상태 유지
                case NavigationStatus.Failed:
                    // 유효한 실패 사유만 외부에 공개
                    if (motorResult.FailureReason != NavigationFailureReason.None)
                    {
                        // Motor 실패 결과 저장
                        _currentResult = NavigationResult.Failed(motorResult.FailureReason);
                    }
                    // 실패 사유가 없을 때 ProviderError 처리
                    else
                    {
                        // 잘못된 실패 payload를 Provider 실패로 변환
                        _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                    }

                    break;

                // 다음 Tick 재탐색 예약
                case NavigationStatus.NeedsRepath:
                    // 재탐색 필요 상태를 즉시 외부에 공개
                    _currentResult = NavigationResult.NeedsRepath();
                    // 다음 Tick에서 한 번 재요청
                    _repathRequested = true;
                    break;

                // 알 수 없는 Motor 상태 차단
                default:
                    // 알 수 없는 상태를 Provider 실패로 변환
                    _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                    break;
            }
        }

        // Grid version 변경을 활성 이동의 재탐색 예약으로 변환
        private void OnGridVersionChanged(int version)
        {
            // 같은 version 통지는 재탐색하지 않음
            if (version == _observedGridVersion)
            {
                // 같은 version 통지 종료
                return;
            }

            // 현재 version 기록
            _observedGridVersion = version;

            // 활성 요청이 없으면 재탐색하지 않음
            if (!_hasRequest)
            {
                // 활성 요청 없는 version 통지 종료
                return;
            }

            // 이동 중이 아니면 terminal 결과를 덮지 않음
            if (_currentResult.Status != NavigationStatus.Moving)
            {
                // terminal 상태 version 통지 종료
                return;
            }

            // 다음 Tick 재탐색 예약
            _repathRequested = true;
            // 재탐색 필요 상태를 즉시 외부에 공개
            _currentResult = NavigationResult.NeedsRepath();
        }

        // 현재 Grid 계약이 모두 준비됐는지 확인
        private bool IsGridInitialized()
        {
            // Metrics와 Navigation 계약이 모두 있는지 반환
            return _gridMetrics != null && _gridNavigation != null;
        }

        // terminal Navigation 결과 여부 확인
        private bool IsTerminalResult()
        {
            // 도착·차단·실패를 terminal 상태로 판정
            return _currentResult.Status == NavigationStatus.Arrived
                || _currentResult.Status == NavigationStatus.Blocked
                || _currentResult.Status == NavigationStatus.Failed;
        }

        // 현재 Motor 이동을 중단하고 예외를 상태로 변환
        private bool TryStopMotor()
        {
            // Motor가 없으면 중단할 작업이 없는 상태
            if (_movementMotor == null)
            {
                // Motor 부재를 호출 성공으로 취급
                return true;
            }

            // Motor Stop Provider 예외를 승인된 실패 결과로 변환
            try
            {
                // 실제 위치 변경 Provider의 이동 중단
                _movementMotor.Stop();
            }
            // Provider 예외 경계
            catch (Exception)
            {
                // Stop 예외를 외부 상태로 변환
                _currentResult = NavigationResult.Failed(NavigationFailureReason.ProviderError);
                // 중단 실패 결과 반환
                return false;
            }

            // 정상 중단 결과 반환
            return true;
        }

        // 이전 Grid version 이벤트 구독 해제
        private void UnsubscribeGridVersion()
        {
            // 구독 상태가 없으면 해제 작업을 생략
            if (!_gridVersionSubscribed)
            {
                // 해제할 Grid version 구독 없음
                return;
            }

            // Metrics가 남아 있을 때만 이벤트 해제
            if (_gridMetrics != null)
            {
                // 이전 Grid version 변경 구독 해제
                _gridMetrics.VersionChanged -= OnGridVersionChanged;
            }

            // 구독 상태 초기화
            _gridVersionSubscribed = false;
        }

        // 유효한 Grid 계약에 version 변경을 중복 없이 구독
        private void SubscribeGridVersion()
        {
            // 이미 구독 중이면 중복 구독을 차단
            if (_gridVersionSubscribed)
            {
                // 기존 Grid version 구독 유지
                return;
            }

            // 두 Grid 계약이 모두 준비되지 않으면 구독하지 않음
            if (!IsGridInitialized())
            {
                // 복구할 Grid version 구독 없음
                return;
            }

            // 현재 Grid version을 재구독 기준으로 저장
            _observedGridVersion = _gridMetrics.Version;
            // Grid version 변경 구독
            _gridMetrics.VersionChanged += OnGridVersionChanged;
            // 구독 상태 저장
            _gridVersionSubscribed = true;
        }
    }
}
