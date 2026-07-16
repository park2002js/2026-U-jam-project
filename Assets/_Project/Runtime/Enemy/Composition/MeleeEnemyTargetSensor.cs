using System.Collections.Generic;
using UJam.Runtime.Combat;
using UJam.Runtime.Grid;
using UJam.Runtime.Navigation;
using UnityEngine;

namespace UJam.Runtime.Enemy.Composition
{
    public sealed class MeleeEnemyTargetSensor : MonoBehaviour, IEnemyTargetConditionProvider
    {
        // 공격 범위 Trigger Collider 연결 슬롯
        [SerializeField] private SphereCollider _attackRangeSensor;
        // 공격 범위 판정 거리
        [SerializeField, Min(0f)] private float _requiredAttackRange = 1.5f;
        // Trigger 대상이 없을 때 이동에 사용할 선택적 Transform
        [SerializeField] private Transform _fallbackNavigationTarget;
        // 이동 통과 능력 설정
        [SerializeField] private bool _canJump;
        // 비행 가능한 이동체인지 여부
        [SerializeField] private bool _canFly;
        // 장애물을 파괴할 수 있는 이동체인지 여부
        [SerializeField] private bool _canBreakObstacles;
        // 현재 Trigger 안의 후보 목록
        private readonly HashSet<HitZoneReceiver> _receivers = new HashSet<HitZoneReceiver>();
        // 외부에서 주입한 Grid 좌표 계약
        private IGridMetrics _gridMetrics;
        // 외부에서 주입한 Grid 통과 계약
        private IGridNavigation _gridNavigation;
        // 런타임 동작 정지 상태
        private bool _isStopped;

        // Unity 활성화 시 Trigger 참조를 보완
        private void Awake()
        {
            // 자식 Sensor를 자동으로 찾지 않고 같은 Component만 보완
            if (_attackRangeSensor == null) _attackRangeSensor = GetComponent<SphereCollider>();
            // Inspector 거리가 음수가 되지 않도록 보정
            if (_requiredAttackRange < 0f) _requiredAttackRange = 0f;
        }

        // Trigger 안으로 들어온 Collider에서 유효 Receiver를 등록
        private void OnTriggerEnter(Collider other)
        {
            // 정지 중에는 새 표적을 받지 않음
            if (_isStopped)
            {
                // 정지 상태 Trigger 진입 무동작
                return;
            }
            // 부모까지 유효 Receiver를 탐색
            HitZoneReceiver receiver = other.GetComponentInParent<HitZoneReceiver>();
            // 자기 자신과 누락 Receiver를 제외
            if (receiver == null || receiver.transform.root == transform.root)
            {
                // 공격 후보가 아닌 Collider 무시
                return;
            }
            // 활성 Receiver만 후보 목록에 추가
            if (receiver.isActiveAndEnabled && receiver.gameObject.activeInHierarchy)
            {
                // 활성 Receiver 등록
                _receivers.Add(receiver);
            }
        }

        // Trigger 밖으로 나간 Collider의 Receiver를 제거
        private void OnTriggerExit(Collider other)
        {
            // 부모까지 Receiver를 탐색
            HitZoneReceiver receiver = other.GetComponentInParent<HitZoneReceiver>();
            // Receiver가 있으면 후보 목록에서 제거
            if (receiver != null)
            {
                // 이탈한 Receiver 제거
                _receivers.Remove(receiver);
            }
        }

        // 현재 가장 가까운 활성 HitZoneReceiver 조건 반환
        public EnemyTargetCondition GetCurrentCondition()
        {
            // 현재 가장 가까운 유효 표적 조회
            HitZoneReceiver receiver = FindClosestReceiver();
            // 유효 표적 존재 여부 계산
            bool hasTarget = receiver != null;
            // 유효 표적이 공격 거리 안인지 계산
            bool isWithinAttackRange = hasTarget && Vector3.Distance(transform.position, receiver.transform.position) <= _requiredAttackRange;
            // 현재 표적 조건 반환
            return new EnemyTargetCondition(hasTarget, isWithinAttackRange);
        }

        // 현재 유효한 가장 가까운 Receiver 반환
        public bool TryGetCurrentTarget(out HitZoneReceiver receiver)
        {
            // 가장 가까운 후보 조회
            receiver = FindClosestReceiver();
            // 표적 존재 결과 반환
            return receiver != null;
        }

        // Grid 주입 시에만 현재 표적 또는 fallback 목적지 요청 생성
        public bool TryCreateNavigationRequest(out NavigationRequest navigationRequest)
        {
            // 출력 요청 초기화
            navigationRequest = default;
            // Grid 계약이 모두 없으면 정상 Pending 반환
            if (_isStopped || _gridMetrics == null || _gridNavigation == null)
            {
                // Grid 미주입 요청 없음 반환
                return false;
            }
            // 공격 표적을 우선 목적지로 선택
            HitZoneReceiver receiver = FindClosestReceiver();
            // 표적이 없을 때만 fallback을 이동 목적지로 사용
            Transform destination = receiver != null ? receiver.transform : _fallbackNavigationTarget;
            // 목적지가 없으면 정상 Pending 반환
            if (destination == null)
            {
                // 목적지 없는 요청 없음 반환
                return false;
            }
            // World 위치를 Grid 목적지로 변환
            GridCell cell = _gridMetrics.WorldToCell(destination.position);
            // Inspector 통과 능력으로 요청 프로필 생성
            TraversalProfile profile = new TraversalProfile(_canJump, _canFly, _canBreakObstacles);
            // 공격 거리 Grid 값 생성
            int requiredDistance = Mathf.Max(0, Mathf.CeilToInt(_requiredAttackRange / Mathf.Max(0.0001f, _gridMetrics.CellSize)));
            // 기존 Navigation 계약 요청 생성
            navigationRequest = new NavigationRequest(cell, profile, new GridDistance(requiredDistance));
            // 요청 생성 성공 반환
            return true;
        }

        // 외부 Grid 계약을 Sensor에 명시적으로 주입
        public void ConfigureNavigation(IGridMetrics gridMetrics, IGridNavigation gridNavigation)
        {
            // 외부에서 받은 Metrics 보관
            _gridMetrics = gridMetrics;
            // 외부에서 받은 Navigation 보관
            _gridNavigation = gridNavigation;
        }

        // 사망 시 Sensor Trigger 후보와 요청을 정지
        public void StopRuntime()
        {
            // 정지 상태를 먼저 기록
            _isStopped = true;
            // 기존 후보 목록 비우기
            _receivers.Clear();
        }

        // 현재 후보 중 가장 가까운 활성 Receiver 조회
        private HitZoneReceiver FindClosestReceiver()
        {
            // 가장 가까운 후보 초기화
            HitZoneReceiver closest = null;
            // 가장 가까운 거리 제곱 초기화
            float closestDistance = float.PositiveInfinity;
            // 후보 목록을 순회
            foreach (HitZoneReceiver receiver in _receivers)
            {
                // 비활성 후보 제거
                if (receiver == null || !receiver.isActiveAndEnabled || !receiver.gameObject.activeInHierarchy)
                {
                    // 비활성 후보를 다음 항목으로 넘김
                    continue;
                }
                // 현재 후보 거리 제곱 계산
                float distance = (receiver.transform.position - transform.position).sqrMagnitude;
                // 더 가까운 후보인지 확인
                if (distance < closestDistance)
                {
                    // 가장 가까운 후보 갱신
                    closestDistance = distance;
                    // 현재 Receiver 보관
                    closest = receiver;
                }
            }
            // 가장 가까운 활성 후보 반환
            return closest;
        }
    }
}
