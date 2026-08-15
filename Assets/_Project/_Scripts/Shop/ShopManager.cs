using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Item;

namespace UJam.Runtime.Shop
{
    public sealed class ShopManager : MonoBehaviour
    {
        [Header("상점에서 판매 가능한 전체 아이템")]
        [SerializeField]
        private ItemData[] itemPool;

        [Header("상점 진열 개수")]
        [SerializeField, Min(1)]
        private int displayCount = 4;

        // 현재 상점에 진열되어 있는 아이템
        private readonly List<ItemData> displayedItems = new();

        // 현재 진열된 아이템을 외부에서 확인하고 싶을 때 사용
        public IReadOnlyList<ItemData> DisplayedItems => displayedItems;


        // ==============================
        // 1. Player가 Shop에 진입
        // ==============================
        public void EnterShop()
        {
            Debug.Log("[Shop] Player가 Shop에 진입했습니다.");

            RefreshShop();
        }


        // ==============================
        // 2. 랜덤 아이템 N개 뽑기
        // ==============================
        private void RefreshShop()
        {
            displayedItems.Clear();

            // ItemData가 등록되어 있는지 확인
            if (itemPool == null || itemPool.Length == 0)
            {
                Debug.LogWarning("[Shop] 등록된 ItemData가 없습니다.");
                return;
            }

            // 진열 개수가 전체 아이템보다 많아지는 것 방지
            int count = Mathf.Min(displayCount, itemPool.Length);

            // 원본 배열을 건드리지 않기 위해 복사
            List<ItemData> tempPool = new List<ItemData>(itemPool);

            for (int i = 0; i < count; i++)
            {
                // 랜덤 인덱스 선택
                int randomIndex = Random.Range(0, tempPool.Count);

                // 해당 아이템 선택
                ItemData selectedItem = tempPool[randomIndex];

                displayedItems.Add(selectedItem);

                // 같은 아이템이 다시 나오지 않도록 제거
                tempPool.RemoveAt(randomIndex);
            }

            PrintShopItems();
        }


        // ==============================
        // 3. 현재 상점 아이템 출력
        // ==============================
        private void PrintShopItems()
        {
            Debug.Log("========== SHOP ==========");

            for (int i = 0; i < displayedItems.Count; i++)
            {
                ItemData item = displayedItems[i];

                Debug.Log(
                    $"Slot {i} | " +
                    $"이름 : {item.DisplayName} | " +
                    $"가격 : {item.Cost}"
                );
            }

            Debug.Log("==========================");
        }


        // ==============================
        // 4. 아이템 구매
        // ==============================
        public bool TryBuyItem(int slotIndex)
        {
            // 잘못된 슬롯 번호 방지
            if (slotIndex < 0 || slotIndex >= displayedItems.Count)
            {
                Debug.LogWarning("[Shop] 존재하지 않는 슬롯입니다.");
                return false;
            }

            ItemData item = displayedItems[slotIndex];

            // Wallet이 Scene에 존재하는지 확인
            if (Wallet.Instance == null)
            {
                Debug.LogError("[Shop] Wallet이 Scene에 존재하지 않습니다.");
                return false;
            }

            Debug.Log(
                $"[Shop] {item.DisplayName} 구매 시도 / 가격 : {item.Cost}"
            );

            // Wallet에게 가격만큼 차감 요청
            bool success = Wallet.Instance.TrySpend(item.Cost);

            // 돈 부족
            if (!success)
            {
                Debug.Log(
                    $"[Shop] 구매 실패 - 재화 부족 / 현재 재화 : " +
                    $"{Wallet.Instance.Currency}"
                );

                return false;
            }

            // 구매 성공
            Debug.Log(
                $"[Shop] 구매 성공 - {item.DisplayName} / " +
                $"남은 재화 : {Wallet.Instance.Currency}"
            );

            return true;
        }


        // ====================================
        // UI 없을 때 테스트하기 위한 함수들
        // ====================================

        [ContextMenu("TEST - Enter Shop")]
        private void TestEnterShop()
        {
            EnterShop();
        }

        [ContextMenu("TEST - Buy Slot 0")]
        private void TestBuySlot0()
        {
            TryBuyItem(0);
        }

        [ContextMenu("TEST - Buy Slot 1")]
        private void TestBuySlot1()
        {
            TryBuyItem(1);
        }
    }
}