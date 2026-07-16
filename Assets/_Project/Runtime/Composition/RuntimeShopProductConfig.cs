using System;
using UnityEngine;
using UJam.Runtime.Shop;

namespace UJam.Runtime.Composition
{
    [Serializable]
    public sealed class RuntimeShopProductConfig
    {
        // Inspector에서 상품 식별자를 저장
        [SerializeField] private string _productId;

        // Inspector에서 상품 가격을 저장
        [SerializeField] private long _price;

        // 설정값으로 순수 런타임 상품을 생성
        public bool TryCreate(out ShopProduct product)
        {
            // 실패 기본값으로 상품 참조를 비움
            product = null;

            // 빈 식별자와 음수 가격을 거부
            if (string.IsNullOrWhiteSpace(_productId) || _price < 0)
            {
                // 잘못된 상품 설정 실패를 반환
                return false;
            }

            // 유효한 상품 식별자와 가격을 구성
            product = new ShopProduct(new ProductId(_productId), new CurrencyAmount(_price));

            // 상품 생성 성공을 반환
            return true;
        }
    }
}
