using UnityEngine;
using UnityEngine.EventSystems;

namespace UJam.Runtime.UI
{
    public class UIShopItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private UIShopItem _item;
        private UIShopBuy _shop;

        private void Awake()
        {
            _item = GetComponentInParent<UIShopItem>();
            _shop = GetComponentInParent<UIShopBuy>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_shop != null && _item != null) _shop.ShowDescription(_item, eventData);
        }

        public void OnPointerExit(PointerEventData eventData) => HideDescription();
        private void OnDisable() => HideDescription();

        private void HideDescription()
        {
            if (_shop != null && _item != null) _shop.HideDescription(_item);
        }
    }
}
