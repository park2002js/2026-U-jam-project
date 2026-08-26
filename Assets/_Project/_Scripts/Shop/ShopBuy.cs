using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Item;
using UJam.Runtime.Player;

namespace UJam.Runtime.Shop
{
    public class ShopBuy
    {
        public static bool IsPlaceholder(string itemId) => string.IsNullOrWhiteSpace(itemId) || itemId == ItemData.NullId;

        // ShopManager의 원본 목록을 공유하며 구매 성공 시에만 원소를 제거한다.
        private readonly List<string> allItemIds;
        private readonly List<string> currentShopItems = new();
        private bool isPurchasing;

        public ShopBuy(List<string> itemIds) => allItemIds = itemIds;

        // 다음 정비에서는 판매 완료 칸만 초기화하고 구매로 줄어든 원본 목록은 유지한다.
        public void BeginPreparation() => currentShopItems.Clear();

        // UI를 다시 열어도 기존 진열 및 Sold Out 상태를 보존한다.
        public List<string> CreateInitialShop(int itemCount) => currentShopItems.Count == itemCount ? new List<string>(currentShopItems) : Reroll(itemCount);

        public List<string> Reroll(int itemCount)
        {
            if (isPurchasing) return new List<string>(currentShopItems);
            for (int i = 0; i < allItemIds.Count; i++)
            {
                int randomIndex = Random.Range(i, allItemIds.Count);
                (allItemIds[i], allItemIds[randomIndex]) = (allItemIds[randomIndex], allItemIds[i]);
            }

            if (currentShopItems.Count != itemCount)
            {
                currentShopItems.Clear();
                for (int i = 0; i < itemCount; i++) currentShopItems.Add(ItemData.NullId);
            }

            int itemIndex = 0;
            for (int slot = 0; slot < currentShopItems.Count; slot++)
            {
                if (currentShopItems[slot] == null) continue; // Sold Out 위치는 리롤 대상에서 제외한다.
                currentShopItems[slot] = itemIndex < allItemIds.Count ? allItemIds[itemIndex++] : ItemData.NullId;
            }
            return new List<string>(currentShopItems);
        }

        public bool BuyItem(string itemId, Wallet wallet, PlayerInventory inventory) => BuyItem(currentShopItems.IndexOf(itemId), wallet, inventory);

        public bool BuyItem(int slot, Wallet wallet, PlayerInventory inventory)
        {
            if (isPurchasing || slot < 0 || slot >= currentShopItems.Count) return false;
            string itemId = currentShopItems[slot];
            if (IsPlaceholder(itemId) || !allItemIds.Contains(itemId)) return false;
            if (wallet == null || inventory == null)
            {
                Debug.LogWarning("[ShopBuy] Wallet과 PlayerInventory 연결을 확인하세요.");
                return false;
            }

            ItemData item = ItemData.Load(itemId);
            if (item == null) return false;
            int cost = item.Cost;
            if (cost < 0 || wallet.Gold < cost || inventory.GetCount(itemId) == int.MaxValue) return false;

            // 재화/보유 변경 이벤트에서 다시 구매하거나 리롤하여 같은 상품을 중복 처리하지 못하게 한다.
            isPurchasing = true;
            try
            {
                if (cost > 0 && !wallet.TrySpend(cost)) return false;
                if (!inventory.TryAdd(itemId))
                {
                    if (cost > 0) wallet.AddCurrency(cost);
                    return false;
                }

                allItemIds.Remove(itemId);
                currentShopItems[slot] = null; // Item_null은 빈 칸, null은 이번 진열의 Sold Out.
                return true;
            }
            finally
            {
                isPurchasing = false;
            }
        }
    }
}
