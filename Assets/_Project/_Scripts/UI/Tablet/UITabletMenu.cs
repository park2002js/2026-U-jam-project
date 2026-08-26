using System;
using UnityEngine;
using UnityEngine.UI;

namespace UJam.Runtime.UI
{
    public enum TabletPage { ItemShop, ItemUpgrade, ItemFusion }

    /// <summary>
    /// Tablet_Menu_UI에 부착하여 메뉴 Button 클릭을 UITablet에 전달하고 실제 열린 화면의 버튼 색상을 유지합니다.
    /// </summary>
    public class UITabletMenu : MonoBehaviour
    {
        [SerializeField] private Button _itemShopButton;
        [SerializeField] private Button _itemUpgradeButton;
        [SerializeField] private Button _itemFusionButton;
        [SerializeField] private Color _selectedColor = new Color(0.45f, 0.75f, 1f, 1f);

        public event Action<TabletPage> OnPageSelected;

        private ColorBlock _shopColors;
        private ColorBlock _upgradeColors;
        private ColorBlock _fusionColors;
        private bool _colorsCached;

        /// <summary>
        /// 각 메뉴 버튼의 클릭 함수를 등록합니다. Inspector의 OnClick에 같은 함수를 중복 연결할 필요는 없습니다.
        /// </summary>
        private void OnEnable()
        {
            if (_itemShopButton != null) _itemShopButton.onClick.AddListener(SelectItemShop);
            if (_itemUpgradeButton != null) _itemUpgradeButton.onClick.AddListener(SelectItemUpgrade);
            if (_itemFusionButton != null) _itemFusionButton.onClick.AddListener(SelectItemFusion);
        }

        /// <summary>
        /// Tablet이 닫힐 때 자신이 등록한 클릭 함수만 해제하여 재활성화 시 중복 실행을 막습니다.
        /// </summary>
        private void OnDisable()
        {
            if (_itemShopButton != null) _itemShopButton.onClick.RemoveListener(SelectItemShop);
            if (_itemUpgradeButton != null) _itemUpgradeButton.onClick.RemoveListener(SelectItemUpgrade);
            if (_itemFusionButton != null) _itemFusionButton.onClick.RemoveListener(SelectItemFusion);
        }

        /// <summary>ItemShop_Btn의 클릭을 UITablet에 전달하여 구매 화면을 엽니다.</summary>
        public void SelectItemShop() => OnPageSelected?.Invoke(TabletPage.ItemShop);

        /// <summary>ItemUpgrade_Btn의 클릭을 UITablet에 전달하여 강화 화면을 엽니다.</summary>
        public void SelectItemUpgrade() => OnPageSelected?.Invoke(TabletPage.ItemUpgrade);

        /// <summary>ItemFusion_Btn의 클릭을 UITablet에 전달하여 합성 화면을 엽니다.</summary>
        public void SelectItemFusion() => OnPageSelected?.Invoke(TabletPage.ItemFusion);

        /// <summary>
        /// UITablet의 초기 선택과 화면 전환을 버튼 색상에 반영합니다. 각 버튼의 원래 색상은 최초 한 번만 저장합니다.
        /// </summary>
        public void SetSelected(TabletPage page)
        {
            if (!_colorsCached)
            {
                if (_itemShopButton != null) _shopColors = _itemShopButton.colors;
                if (_itemUpgradeButton != null) _upgradeColors = _itemUpgradeButton.colors;
                if (_itemFusionButton != null) _fusionColors = _itemFusionButton.colors;
                _colorsCached = true;
            }
            SetButtonColor(_itemShopButton, _shopColors, page == TabletPage.ItemShop);
            SetButtonColor(_itemUpgradeButton, _upgradeColors, page == TabletPage.ItemUpgrade);
            SetButtonColor(_itemFusionButton, _fusionColors, page == TabletPage.ItemFusion);
        }

        /// <summary>
        /// Button의 Color Tint를 변경하여 포인터가 떠나거나 다른 UI가 포커스를 받아도 열린 메뉴의 선택 색을 유지합니다.
        /// </summary>
        private void SetButtonColor(Button button, ColorBlock original, bool selected)
        {
            if (button == null) return;
            ColorBlock colors = original;
            if (selected)
            {
                colors.normalColor = _selectedColor;
                colors.highlightedColor = _selectedColor;
                colors.pressedColor = _selectedColor;
                colors.selectedColor = _selectedColor;
            }
            else colors.selectedColor = original.normalColor;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;
        }
    }
}
