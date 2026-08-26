using System;
using System.Collections.Generic;
using UnityEngine;
using UJam.Runtime.Item;

namespace UJam.Runtime.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        // 아이템의 식별 Id와 
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        public event Action OnItemsChanged;
        public IReadOnlyDictionary<string, int> Items => _items;

        /// <summary>
        /// 아이템 사용 코드에서 지정한 ID의 현재 보유 수량을 조회합니다.
        /// </summary>
        public int GetCount(string itemId)
        {
            // 비어 있는 Item 식별자 차단
            if (string.IsNullOrWhiteSpace(itemId))
            {
                // 보유하지 않은 수량 반환
                return 0;
            }

            // 저장된 Item 수량 확인
            int count;
            // 존재하는 Item 수량 반환
            return _items.TryGetValue(itemId, out count) ? count : 0;
        }

        /// <summary>
        /// 아이템을 추가하고 성공한 경우에만 UIPlayerItems에 목록 변경을 알립니다.
        /// </summary>
        public bool TryAdd(string itemId, int amount = 1)
        {
            // 비어 있는 ID와 양수가 아닌 수량 차단
            if (string.IsNullOrWhiteSpace(itemId) || itemId == ItemData.NullId || amount <= 0)
            {
                // Item 추가 실패 반환
                return false;
            }

            // 현재 보유 수량 조회
            int current = GetCount(itemId);
            // int 범위를 넘는 수량 차단
            if (current > int.MaxValue - amount)
            {
                // Item 추가 실패 반환
                return false;
            }

            // 새 보유 수량 저장
            _items[itemId] = current + amount;
            OnItemsChanged?.Invoke();

            // Item 추가 성공 반환
            return true;
        }

        /// <summary>
        /// 아이템을 제거하고 성공한 경우에만 UIPlayerItems에 목록 변경을 알립니다.
        /// </summary>
        public bool TryRemove(string itemId, int amount = 1)
        {
            // 비어 있는 ID와 양수가 아닌 수량 차단
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
            {
                // Item 제거 실패 반환
                return false;
            }

            // 현재 보유 수량 조회
            int current = GetCount(itemId);
            // 부족한 수량 차단
            if (current < amount)
            {
                // Item 제거 실패 반환
                return false;
            }

            // 제거 뒤 남을 수량 계산
            int remaining = current - amount;
            // 마지막 Item이면 장부에서 제거
            if (remaining == 0)
            {
                _items.Remove(itemId);
            }
            else
            {
                _items[itemId] = remaining;
            }

            OnItemsChanged?.Invoke();

            // Item 제거 성공 반환
            return true;
        }
    }
}
