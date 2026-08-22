using System.Collections.Generic;
using UnityEngine;

namespace ItemShopSystem
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        // 외부 시스템
        //private Wallet wallet;

        // ShopManager가 관리하는 세부 기능
        private ShopBuy shopBuy;
        private ShopFusion shopFusion;
        private ShopUpgrade shopUpgrade;

        // 임시 아이템 데이터
        // 나중에 실제 ItemData 목록으로 교체
        private List<string> implementedItemIds = new List<string>()
        {
            "Item_001",
            "Item_002",
            "Item_003",
            "Item_004",
            "Item_005"
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            shopBuy = new ShopBuy(implementedItemIds);
            shopFusion = new ShopFusion();
            shopUpgrade = new ShopUpgrade();

            Debug.Log("[ShopManager] 초기화");
        }


        // ==========================
        // 상점 초기 생성
        // ==========================

        public List<string> OpenShop(int count)
        {
            return shopBuy.CreateInitialShop(count);
        }


        // ==========================
        // 리롤
        // ==========================

        public List<string> Reroll(int count)
        {
            return shopBuy.Reroll(count);
        }


        // ==========================
        // 구매
        // ==========================

        public bool BuyItem(string itemId)
        {
            return shopBuy.BuyItem(itemId);
        }


        // ==========================
        // 조합
        // ==========================

        public string FuseItem(string itemId1, string itemId2)
        {
            if (!ValidateItem(itemId1, itemId2))
                return null;

            return shopFusion.Fuse(itemId1, itemId2);
        }


        // ==========================
        // 강화
        // ==========================

        public string UpgradeItem(string itemId)
        {
            if (!ValidateItem(itemId))
                return null;

            return shopUpgrade.Upgrade(itemId);
        }


        // ==========================
        // 공통 유효성 검사
        // ==========================

        private bool ValidateItem(params string[] itemIds)
        {
            Debug.Log("[ShopManager] 아이템 유효성 검사");

            // TODO
            // ItemData 및 Inventory 구현 후 연결

            return true;
        }
    }
}