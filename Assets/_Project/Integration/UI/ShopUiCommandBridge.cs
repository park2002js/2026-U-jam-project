using System;
using UnityEngine;
using UJam.Runtime.Phase;
using UJam.Runtime.Shop;

namespace UJam.Integration.UI
{
    public sealed class ShopUiCommandBridge : MonoBehaviour
    {
        // 마지막 구매 결과 보관
        public PurchaseResult LastResult { get; private set; }

        // 마지막 구매 요청 ID 보관
        public string LastRequestId { get; private set; } = string.Empty;

        // 명시적으로 주입된 ShopSystem 보관
        private ShopSystem _shopSystem;

        // 명시적으로 주입된 PhaseSystem 보관
        private PhaseSystem _phaseSystem;

        // ShopSystem과 PhaseSystem을 명시적으로 주입
        public void Configure(ShopSystem shopSystem, PhaseSystem phaseSystem)
        {
            // ShopSystem 연결 저장
            _shopSystem = shopSystem;

            // PhaseSystem 연결 저장
            _phaseSystem = phaseSystem;
        }

        // 상품 구매 명령 전달
        public void PurchaseProduct(string productId)
        {
            // 새 UI 호출의 기본 결과 초기화
            LastResult = null;
            LastRequestId = string.Empty;

            // 의존성 누락 여부 확인
            if (_shopSystem == null || _phaseSystem == null)
            {
                // 의존성 누락 상태 유지
                return;
            }

            // 상품 ID 공백 여부 확인
            if (string.IsNullOrWhiteSpace(productId))
            {
                // 빈 상품 ID 상태 유지
                return;
            }

            // 유효한 구매 요청 ID를 한 번 생성
            string requestId = Guid.NewGuid().ToString("N");

            // 현재 Phase를 읽어 구매 요청 구성
            PurchaseRequest request = new PurchaseRequest(
                new ProductId(productId),
                requestId,
                _phaseSystem.CurrentState);

            // 마지막 요청 ID 저장
            LastRequestId = requestId;

            // Runtime 구매 명령을 한 번 전달
            LastResult = _shopSystem.Purchase(request);
        }
    }
}
