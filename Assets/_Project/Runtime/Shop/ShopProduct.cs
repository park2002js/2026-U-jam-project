using System;

namespace UJam.Runtime.Shop
{
    public sealed class ShopProduct
    {
        // 상품 식별자와 가격 기반 상품 생성
        public ShopProduct(ProductId productId, CurrencyAmount price)
        {
            // 유효하지 않은 상품 식별자 차단
            if (!productId.IsValid)
            {
                // 생성자 입력 오류 알림
                throw new ArgumentException("ProductId must be valid.", nameof(productId));
            }

            // 검증 상품 식별자 저장
            ProductId = productId;

            // 검증 상품 가격 저장
            Price = price;
        }

        // 상품 식별자 조회
        public ProductId ProductId { get; }

        // 상품 가격 조회
        public CurrencyAmount Price { get; }
    }
}
