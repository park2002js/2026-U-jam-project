using System;
using System.Collections.Generic;

namespace UJam.Runtime.Shop
{
    public sealed class ShopCatalog
    {
        // 상품 식별자 기반 불변 매핑
        private readonly Dictionary<ProductId, ShopProduct> _products;

        // 명시 상품 목록 검증과 복사
        public ShopCatalog(IEnumerable<ShopProduct> products)
        {
            // 빈 상품 목록 생성 차단
            if (products == null)
            {
                // null 목록 입력 오류 알림
                throw new ArgumentNullException(nameof(products));
            }

            // 상품 저장용 새 매핑 생성
            _products = new Dictionary<ProductId, ShopProduct>();

            // 입력 상품 단위 검증
            foreach (ShopProduct product in products)
            {
                // null 상품 입력 차단
                if (product == null)
                {
                    // null 상품 입력 오류 알림
                    throw new ArgumentException("Catalog product must not be null.", nameof(products));
                }

                // 중복 상품 식별자 차단
                if (!_products.TryAdd(product.ProductId, product))
                {
                    // 중복 상품 입력 오류 알림
                    throw new ArgumentException("Catalog product identifiers must be unique.", nameof(products));
                }
            }
        }

        // 상품 식별자 기반 상품 조회
        public bool TryGet(ProductId productId, out ShopProduct product)
        {
            // 매핑 상품 반환
            return _products.TryGetValue(productId, out product);
        }
    }
}
