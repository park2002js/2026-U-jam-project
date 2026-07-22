using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Player
{
    public sealed class PlayerInventory : MonoBehaviour
    {
        // Item 정의가 확정되기 전 문자열 식별자와 수량 보관
        private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

        // 지정한 Item의 현재 보유 수량 조회
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

        // Item과 수량을 보관소에 추가
        public bool TryAdd(string itemId, int amount = 1)
        {
            // 비어 있는 ID와 양수가 아닌 수량 차단
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
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

            // Item 추가 성공 반환
            return true;
        }

        // Item과 수량을 보관소에서 제거
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

            // Item 제거 성공 반환
            return true;
        }
    }
}
