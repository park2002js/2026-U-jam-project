using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Player;
using Ujam.Runtime.Item;

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

        /// <summary>Unity가 게임 시작 시 호출한다. 대표 ShopManager를 등록하고 상점 데이터를 초기화한다.</summary>
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

        /// <summary>Catalog의 전체 ID를 상점용 목록에 복사하고 ShopBuy에 전달한다. 구매/조합/강화 기능을 준비한 뒤 초기화 완료를 표시한다.</summary>
        private void Initialize()
        {
            implementedItemIds.AddRange(ItemCatalog.IDs);
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

        /// <summary>UI가 상점을 열 때 호출한다. 최초에는 상품 ID를 선정하고, 이후에는 현재 진열 목록을 반환한다.</summary>
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
