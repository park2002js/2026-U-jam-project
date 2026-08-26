using UnityEngine;

namespace UJam.Runtime.UI
{
    /// <summary>
    /// Tablet_Frame_UI에 부착하여 UIManager가 Tablet을 켤 때 상점을 초기화하고 메뉴별 화면을 전환합니다.
    /// </summary>
    public class UITablet : MonoBehaviour
    {
        [SerializeField] private UITabletMenu _menu;
        [SerializeField] private UIShopBuy _itemShop;
        [SerializeField] private GameObject _itemUpgrade;
        [SerializeField] private GameObject _itemFusion;

        private bool _shopInitialized;

        /// <summary>
        /// Tablet 활성화 시 구매 화면을 기본 선택하고 기존 진열을 표시합니다. 첫 진열만 새로 생성합니다.
        /// </summary>
        private void OnEnable()
        {
            if (_menu != null) _menu.OnPageSelected += ShowPage;
            ShowPage(TabletPage.ItemShop);
            _shopInitialized = _itemShop != null && _itemShop.InitializeShop();
        }

        /// <summary>
        /// 첫 OnEnable보다 ShopManager.Awake가 늦었을 때만 모든 Awake 이후 상점 초기화를 다시 시도합니다.
        /// </summary>
        private void Start()
        {
            if (!_shopInitialized && _itemShop != null) _shopInitialized = _itemShop.InitializeShop();
            if (!_shopInitialized) Debug.LogWarning("[UITablet] UIShopBuy 설정과 활성화된 ItemShopSystem.ShopManager를 확인하세요.", this);
        }

        /// <summary>
        /// Tablet을 닫으면 메뉴 구독만 해제합니다. 진열 초기화는 GameManager의 다음 정비 진입에서 처리합니다.
        /// </summary>
        private void OnDisable()
        {
            if (_menu != null) _menu.OnPageSelected -= ShowPage;
            _shopInitialized = false;
        }

        /// <summary>
        /// UITabletMenu의 선택에 맞는 화면만 켜고 버튼 색상을 갱신합니다. 메뉴 전환만으로 상품을 다시 뽑지 않습니다.
        /// </summary>
        private void ShowPage(TabletPage page)
        {
            if (_itemShop != null) _itemShop.gameObject.SetActive(page == TabletPage.ItemShop);
            if (_itemUpgrade != null) _itemUpgrade.SetActive(page == TabletPage.ItemUpgrade);
            if (_itemFusion != null) _itemFusion.SetActive(page == TabletPage.ItemFusion);
            if (_menu != null) _menu.SetSelected(page);
        }
    }
}
