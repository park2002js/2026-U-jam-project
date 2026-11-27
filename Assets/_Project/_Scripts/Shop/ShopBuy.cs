using System.Collections.Generic;
using UnityEngine;
using Ujam.Runtime.Item;
using UJam.Runtime.Player;
using UJam.Runtime.Systems;

namespace UJam.Runtime.Shop
{
    /// <summary>상점 구매 시도 결과입니다.</summary>
    public enum PurchaseResult { Failed, Purchased, NeedsSkillReplacement }

    public class ShopBuy
    {
        public static bool IsPlaceholder(string itemId) => string.IsNullOrWhiteSpace(itemId) || itemId == Item.NullId;

        // ShopManager의 원본 목록을 공유하며 구매 성공 시에만 원소를 제거한다.
        private readonly List<string> allItemIds;
        private readonly List<string> currentShopItems = new();
        private readonly AudioClip purchaseClip;
        private bool isPurchasing;

        /// <summary>ShopManager가 Catalog에서 받아온 상점용 ID 목록을 저장한다. 새 복사본을 만들지 않고 같은 목록을 공유한다.</summary>
        public ShopBuy(List<string> itemIds)
        {
            allItemIds = itemIds;
            purchaseClip = Resources.Load<AudioClip>("Audio/Game/ShopPurchase");
        }

        // 다음 정비에서는 판매 완료 칸만 초기화하고 구매로 줄어든 원본 목록은 유지한다.
        public void BeginPreparation() => currentShopItems.Clear();

        // UI를 다시 열어도 기존 진열 및 Sold Out 상태를 보존한다.
        public List<string> CreateInitialShop(int itemCount) => currentShopItems.Count == itemCount ? new List<string>(currentShopItems) : Reroll(itemCount);

        /// <summary>최초 진열 또는 리롤 시 남은 판매 후보 ID를 섞어 진열 칸을 채운다. 후보가 부족하면 Item_null을 넣고, 판매 완료 칸은 유지한다.</summary>
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
                for (int i = 0; i < itemCount; i++) currentShopItems.Add(Item.NullId);
            }

            int itemIndex = 0;
            for (int slot = 0; slot < currentShopItems.Count; slot++)
            {
                if (currentShopItems[slot] == null) continue; // Sold Out 위치는 리롤 대상에서 제외한다.
                currentShopItems[slot] = itemIndex < allItemIds.Count ? allItemIds[itemIndex++] : Item.NullId;
            }
            return new List<string>(currentShopItems);
        }

        public bool BuyItem(string itemId, Wallet wallet, PlayerInventory inventory) => BuyItem(currentShopItems.IndexOf(itemId), wallet, inventory);

        /// <summary>
        /// 구매를 시도합니다. 액티브 아이템인데 스킬칸이 모두 차 있으면 재화를 쓰지 않고 NeedsSkillReplacement를 반환하며,
        /// UI가 교체할 스킬칸을 고른 뒤 BuyItemReplacing으로 구매를 마칩니다.
        /// </summary>
        public PurchaseResult TryBuy(int slot, Wallet wallet, PlayerInventory inventory)
        {
            if (!Validate(slot, wallet, inventory, out string itemId, out _)) return PurchaseResult.Failed;
            if (inventory.NeedsSkillReplacement(itemId)) return PurchaseResult.NeedsSkillReplacement;
            return BuyItem(slot, wallet, inventory) ? PurchaseResult.Purchased : PurchaseResult.Failed;
        }

        /// <summary>
        /// 지정한 스킬칸의 아이템을 제거하고 그 자리에 상품을 장착합니다. 제거된 아이템은 다시 상점 후보로 돌아갑니다.
        /// </summary>
        public bool BuyItemReplacing(int slot, int skillSlot, Wallet wallet, PlayerInventory inventory)
        {
            if (!Validate(slot, wallet, inventory, out string itemId, out int cost)) return false;

            isPurchasing = true;
            try
            {
                if (cost > 0 && !wallet.TrySpend(cost)) return false;
                if (!inventory.TryReplaceSkill(skillSlot, itemId, out string removedId))
                {
                    if (cost > 0) wallet.Refund(cost);
                    return false;
                }

                allItemIds.Remove(itemId);
                if (!IsPlaceholder(removedId) && !allItemIds.Contains(removedId)) allItemIds.Add(removedId);
                currentShopItems[slot] = null;
                var sfx = SFXManager.Instance;
                if (sfx != null) sfx.Play(purchaseClip);
                return true;
            }
            finally
            {
                isPurchasing = false;
            }
        }

        /// <summary>구매 가능한 상품·재화·연결 상태인지 확인합니다. 실제 재화 차감은 하지 않습니다.</summary>
        private bool Validate(int slot, Wallet wallet, PlayerInventory inventory, out string itemId, out int cost)
        {
            itemId = null;
            cost = 0;
            if (isPurchasing || slot < 0 || slot >= currentShopItems.Count) return false;
            itemId = currentShopItems[slot];
            if (IsPlaceholder(itemId) || !allItemIds.Contains(itemId)) return false;
            if (wallet == null || inventory == null)
            {
                Debug.LogWarning("[ShopBuy] Wallet과 PlayerInventory 연결을 확인하세요.");
                return false;
            }

            ItemMeta item = ItemCatalog.GetMeta(itemId);
            if (item == null) return false;
            cost = item.Price;
            return cost >= 0 && wallet.Gold >= cost && inventory.GetCount(itemId) != int.MaxValue;
        }

        public bool BuyItem(int slot, Wallet wallet, PlayerInventory inventory)
        {
            if (!Validate(slot, wallet, inventory, out string itemId, out int cost)) return false;

            // 재화/보유 변경 이벤트에서 다시 구매하거나 리롤하여 같은 상품을 중복 처리하지 못하게 한다.
            isPurchasing = true;
            try
            {
                if (cost > 0 && !wallet.TrySpend(cost)) return false;
                if (!inventory.TryAdd(itemId))
                {
                    if (cost > 0) wallet.Refund(cost);
                    return false;
                }

                allItemIds.Remove(itemId);
                currentShopItems[slot] = null; // Item_null은 빈 칸, null은 이번 진열의 Sold Out.
                var sfx = SFXManager.Instance;
                if (sfx != null) sfx.Play(purchaseClip);
                return true;
            }
            finally
            {
                isPurchasing = false;
            }
        }
    }
}
