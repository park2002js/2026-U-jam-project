using System;

namespace UJam.Runtime.Shop
{
    public sealed class PurchaseOrder
    {
        // 배치에 전달할 주문 식별 정보를 검증하고 저장
        private PurchaseOrder(string orderId, ProductId productId)
        {
            // 비어 있는 주문 식별자 차단
            if (string.IsNullOrWhiteSpace(orderId))
            {
                throw new ArgumentException("OrderId must not be empty.", nameof(orderId));
            }

            // 저장할 상품 식별자 유효성 확인
            if (!productId.IsValid)
            {
                throw new ArgumentException("ProductId must be valid.", nameof(productId));
            }

            OrderId = orderId;
            ProductId = productId;
        }

        // Shop Runtime이 유효한 주문 생성
        internal static PurchaseOrder Create(string orderId, ProductId productId)
        {
            return new PurchaseOrder(orderId, productId);
        }

        // Placement가 소비할 주문 식별자
        public string OrderId { get; }

        // Placement가 소비할 상품 식별자
        public ProductId ProductId { get; }
    }
}
