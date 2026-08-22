using System.Collections.Generic;
using UnityEngine;

namespace ItemShopSystem
{
    public class ShopBuy
    {
        // 게임에 구현된 전체 아이템
        private List<string> allItemIds;

        // 현재 상점에 떠 있는 아이템
        private List<string> currentShopItems;


        public ShopBuy(List<string> itemIds)
        {
            allItemIds = new List<string>(itemIds);
            currentShopItems = new List<string>();
        }


        // ============================================================
        // 최초 상점 생성
        // ============================================================

        public List<string> CreateInitialShop(int itemCount)
        {
            Debug.Log("[ShopBuy] 초기 상점 생성");

            List<string> shuffledItems =
                new List<string>(allItemIds);

            Shuffle(shuffledItems);

            currentShopItems.Clear();

            for (int i = 0; i < itemCount; i++)
            {
                if (i < shuffledItems.Count)
                {
                    currentShopItems.Add(shuffledItems[i]);
                }
                else
                {
                    // 아이템 부족 시 null
                    currentShopItems.Add(null);
                }
            }

            return new List<string>(currentShopItems);
        }


        // ============================================================
        // 리롤
        // ============================================================

        public List<string> Reroll(int itemCount)
        {
            Debug.Log($"[ShopBuy] {itemCount}개 리롤");

            List<string> result =
                new List<string>();

            for (int i = 0; i < itemCount; i++)
            {
                if (allItemIds.Count == 0)
                {
                    result.Add(null);
                    continue;
                }

                int randomIndex =
                    Random.Range(0, allItemIds.Count);

                result.Add(allItemIds[randomIndex]);
            }

            currentShopItems = new List<string>(result);

            return result;
        }


        // ============================================================
        // 구매
        // ============================================================

        public bool BuyItem(string itemId)
        {
            // 현재 상점 목록에 있는 아이템인지 확인
            if (!currentShopItems.Contains(itemId))
            {
                Debug.LogError(
                    $"[ShopBuy] 현재 상점에 없는 아이템입니다 : {itemId}"
                );

                return false;
            }


            // =========================================
            // TODO : Wallet 연결
            // =========================================

            Debug.Log(
                $"[ShopBuy] Wallet 재화 검사 예정 : {itemId}"
            );


            // 현재는 구매 성공했다고 가정

            currentShopItems.Remove(itemId);


            // =========================================
            // TODO : 실제 Inventory 추가
            // =========================================

            Debug.Log(
                $"[ShopBuy] 구매 성공 : {itemId}"
            );

            return true;
        }


        // ============================================================
        // 리스트 섞기
        // ============================================================

        private void Shuffle(List<string> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex =
                    Random.Range(i, list.Count);

                string temp = list[i];

                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }
    }
}