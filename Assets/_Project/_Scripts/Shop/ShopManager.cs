using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Player;

namespace UJam.Runtime.Shop
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }
        public bool IsInitialized { get; private set; }

        // ShopManager가 관리하는 세부 기능
        private ShopBuy shopBuy;
        private ShopFusion shopFusion;
        private ShopUpgrade shopUpgrade;

        private readonly List<string> implementedItemIds = new();
        [SerializeField] private PlayerInventory playerInventory;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            Initialize();
        }

        private void Initialize()
        {
            // 추후에 아이템 아이디 리스트 파일을 읽고 리스트를 채울 것
            implementedItemIds.Add("Item_001");
            if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
            shopBuy = new ShopBuy(implementedItemIds);
            shopFusion = new ShopFusion();
            shopUpgrade = new ShopUpgrade();
            IsInitialized = true;
            Debug.Log("[ShopManager] 초기화");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            IsInitialized = false;
        }

        // ==========================
        // 상점 초기 생성
        // ==========================

        public List<string> OpenShop(int count)
        {
            return shopBuy.CreateInitialShop(count);
        }

        public void BeginPreparation()
        {
            if (IsInitialized) shopBuy.BeginPreparation();
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
            return IsInitialized && shopBuy.BuyItem(itemId, Wallet.Instance, playerInventory);
        }

        public bool BuyItem(int slot) => IsInitialized && shopBuy.BuyItem(slot, Wallet.Instance, playerInventory);


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
