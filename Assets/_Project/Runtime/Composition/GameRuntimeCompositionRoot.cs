using System;
using System.Collections.Generic;
using UnityEngine;
using UJam.Integration.UI;
using UJam.Runtime.BuildingPlacement;
using UJam.Runtime.Defense;
using UJam.Runtime.Enemy.Composition;
using UJam.Runtime.Grid;
using UJam.Runtime.Phase;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;

namespace UJam.Runtime.Composition
{
    public sealed class GameRuntimeCompositionRoot : MonoBehaviour
    {
        // Stage 식별자를 Inspector에서 저장
        [SerializeField] private int _stageId;

        // Grid Cell 크기를 Inspector에서 저장
        [SerializeField] private float _gridCellSize = 1f;

        // Grid 원점 좌표를 Inspector에서 저장
        [SerializeField] private Vector3 _gridOrigin;

        // Grid 가로 Cell 수를 Inspector에서 저장
        [SerializeField] private int _gridWidth = 10;

        // Grid 세로 Cell 수를 Inspector에서 저장
        [SerializeField] private int _gridHeight = 10;

        // 기본 Cell 통행 가능 여부를 Inspector에서 저장
        [SerializeField] private bool _defaultPassable = true;

        // 기본 Cell 이동 비용을 Inspector에서 저장
        [SerializeField] private float _defaultMovementCost = 1f;

        // 시작 Wallet 잔액을 Inspector에서 저장
        [SerializeField] private long _initialWalletBalance;

        // 상점 상품 설정 배열을 Inspector에서 저장
        [SerializeField] private RuntimeShopProductConfig[] _productConfigs;

        // Barricade 생성 경계를 Inspector에서 연결
        [SerializeField] private BarricadeFactory _barricadeFactory;

        // Player 런타임 주입 경계를 Inspector에서 연결
        [SerializeField] private PlayerRuntimeBinder _playerRuntimeBinder;

        // 읽기 전용 UI 상태 경계를 Inspector에서 연결
        [SerializeField] private RuntimeUiStateBridge _runtimeUiStateBridge;

        // Phase UI 명령 경계를 Inspector에서 연결
        [SerializeField] private PhaseUiCommandBridge _phaseUiCommandBridge;

        // Shop UI 명령 경계를 Inspector에서 연결
        [SerializeField] private ShopUiCommandBridge _shopUiCommandBridge;

        // 선택적 Player 공격 대상 Provider Component를 Inspector에서 연결
        [SerializeField] private MonoBehaviour _playerTargetProvider;

        // 조립 성공 여부를 저장
        private bool _isInitialized;

        // 생성된 GridSystem을 내부 주입용으로 보관
        private GridSystem _gridSystem;

        // 생성된 PhaseSystem을 내부 주입용으로 보관
        private PhaseSystem _phaseSystem;

        // 생성된 Wallet을 내부 주입용으로 보관
        private Wallet _wallet;

        // 생성된 ShopSystem을 내부 주입용으로 보관
        private ShopSystem _shopSystem;

        // 생성된 BuildingPlacementSystem을 내부 주입용으로 보관
        private BuildingPlacementSystem _buildingPlacementSystem;

        // 초기화 성공 상태를 조회
        public bool IsInitialized
        {
            get
            {
                // 현재 조립 성공 상태 반환
                return _isInitialized;
            }
        }

        // Unity 활성화 시점에 런타임 조립을 시도
        private void Awake()
        {
            // Inspector 설정으로 한 번 초기화
            Initialize();
        }

        // 모든 설정과 참조를 검증한 뒤 런타임 시스템을 한 번 조립
        public bool Initialize()
        {
            // 성공한 조립은 재생성하거나 재주입하지 않고 유지
            if (_isInitialized)
            {
                // 현재 성공 상태 반환
                return true;
            }

            // 검증된 상품 목록을 받을 지역 변수 준비
            List<ShopProduct> products;

            // 설정과 필수 참조를 조립 전에 모두 검증
            if (!ValidateConfiguration(out products))
            {
                // 잘못된 조립 설정 실패를 반환
                return false;
            }

            // 검증된 순서대로 런타임 시스템을 한 번 생성
            // GridSystem 생성
            GridSystem gridSystem = new GridSystem(
                _gridCellSize,
                _gridOrigin,
                _gridWidth,
                _gridHeight,
                _defaultPassable,
                _defaultMovementCost);
            // PhaseSystem 생성
            PhaseSystem phaseSystem = new PhaseSystem(_stageId);
            // Wallet 생성
            Wallet wallet = new Wallet(new CurrencyAmount(_initialWalletBalance));
            // ShopCatalog 생성
            ShopCatalog shopCatalog = new ShopCatalog(products);
            // ShopSystem 생성
            ShopSystem shopSystem = new ShopSystem(shopCatalog, wallet);
            // 생성된 배치 시스템을 내부에서 보관
            _buildingPlacementSystem = new BuildingPlacementSystem(
                gridSystem,
                gridSystem,
                _barricadeFactory);

            // 생성된 시스템을 내부 필드에 보관
            _gridSystem = gridSystem;
            _phaseSystem = phaseSystem;
            _wallet = wallet;
            _shopSystem = shopSystem;

            // Defense에 Grid 점유 경계를 한 번 주입
            _barricadeFactory.ConfigureGridOccupancy(gridSystem);

            // Player에 Phase, Grid, 선택 Provider를 한 번 주입
            _playerRuntimeBinder.ConfigurePhaseSystem(phaseSystem);
            _playerRuntimeBinder.ConfigureGridAreaQuery(gridSystem);
            _playerRuntimeBinder.ConfigureAttackTargetProvider(GetTargetProviderOrNull(_playerTargetProvider));

            // UI Bridge에 생성 시스템을 한 번 주입
            _runtimeUiStateBridge.Configure(phaseSystem, wallet, _playerRuntimeBinder);
            _phaseUiCommandBridge.ConfigurePhaseSystem(phaseSystem);
            _shopUiCommandBridge.Configure(shopSystem, phaseSystem);

            // 조립 성공 상태를 마지막에 확정
            _isInitialized = true;

            // 조립 성공을 반환
            return true;
        }

        // 초기화된 적 Binder에 Grid Metrics와 Navigation을 주입
        public bool ConfigureMeleeEnemy(MeleeEnemyRuntimeBinder binder)
        {
            // 초기화 전이나 대상이 없으면 적을 건드리지 않음
            if (!_isInitialized || binder == null)
            {
                // 적 주입 실패를 반환
                return false;
            }

            // 생성된 Grid 계약을 적 Binder에 한 번 주입
            binder.ConfigureNavigation(_gridSystem, _gridSystem);

            // 적 주입 성공을 반환
            return true;
        }

        // 초기화된 Player에 선택적 공격 대상 Provider를 주입
        public bool ConfigurePlayerAttackTargetProvider(MonoBehaviour providerComponent)
        {
            // 초기화 전에는 Player 주입을 수행하지 않음
            if (!_isInitialized)
            {
                // Player 주입 실패를 반환
                return false;
            }

            // null 또는 올바른 인터페이스 Provider만 허용
            // Provider Component를 인터페이스로 변환
            IPlayerAttackTargetProvider provider = GetTargetProviderOrNull(providerComponent);
            _playerRuntimeBinder.ConfigureAttackTargetProvider(provider);

            // 잘못된 Component는 null 처리 후 실패를 반환
            return providerComponent == null || provider != null;
        }

        // Inspector 설정과 필수 참조를 생성 전에 검증
        private bool ValidateConfiguration(out List<ShopProduct> products)
        {
            // 실패 기본값으로 상품 목록을 준비
            products = null;

            // Stage, Grid, Wallet 수치의 유효성을 검증
            if (_stageId < 0
                || !IsPositiveFinite(_gridCellSize)
                || !IsFinite(_gridOrigin)
                || _gridWidth <= 0
                || _gridHeight <= 0
                || !IsPositiveFinite(_defaultMovementCost)
                || _initialWalletBalance < 0)
            {
                // 잘못된 기본 설정 실패를 반환
                return false;
            }

            // 모든 필수 Scene 참조의 연결 여부를 검증
            if (_productConfigs == null
                || _barricadeFactory == null
                || _playerRuntimeBinder == null
                || _runtimeUiStateBridge == null
                || _phaseUiCommandBridge == null
                || _shopUiCommandBridge == null)
            {
                // 필수 참조 누락 실패를 반환
                return false;
            }

            // 상품 설정을 순회하며 런타임 상품으로 변환
            products = new List<ShopProduct>(_productConfigs.Length);
            // 상품 중복 확인용 식별자 집합 생성
            HashSet<ProductId> productIds = new HashSet<ProductId>();
            foreach (RuntimeShopProductConfig config in _productConfigs)
            {
                // 현재 설정에서 생성할 상품 지역 변수 준비
                ShopProduct product;

                // null 설정과 상품 생성 실패를 거부
                if (config == null || !config.TryCreate(out product))
                {
                    // 상품 설정 실패를 반환
                    products = null;
                    return false;
                }

                // 중복 상품 식별자를 생성 전에 거부
                if (!productIds.Add(product.ProductId))
                {
                    // 중복 상품 실패를 반환
                    products = null;
                    return false;
                }

                // 유효한 상품을 조립 목록에 추가
                products.Add(product);
            }

            // 전체 설정 검증 성공을 반환
            return true;
        }

        // 유한한 양수인지 검증
        private static bool IsPositiveFinite(float value)
        {
            // 양수이며 무한하지 않은 값인지 반환
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        // Vector3 각 좌표가 유한한지 검증
        private static bool IsFinite(Vector3 value)
        {
            // 모든 좌표의 유한 여부 반환
            return !float.IsNaN(value.x)
                && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y)
                && !float.IsInfinity(value.y)
                && !float.IsNaN(value.z)
                && !float.IsInfinity(value.z);
        }

        // Provider Component가 인터페이스를 구현하면 반환하고 아니면 null 반환
        private static IPlayerAttackTargetProvider GetTargetProviderOrNull(MonoBehaviour providerComponent)
        {
            // 올바른 Component만 Provider로 변환
            return providerComponent as IPlayerAttackTargetProvider;
        }
    }
}
