using System;
using UJam.Runtime.Phase;

namespace UJam.Runtime.Shop
{
    public readonly struct ProductId : IEquatable<ProductId>
    {
        // 상품 식별자를 검증하고 저장
        public ProductId(string value)
        {
            // 비어 있는 상품 식별자 차단
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("ProductId must not be empty.", nameof(value));
            }

            Value = value;
        }

        // 원본 상품 식별자
        public string Value { get; }

        // 상품 식별자 유효성 결과
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        // 상품 식별자 값 비교
        public bool Equals(ProductId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        // 객체가 ProductId인지 확인하고 값 비교
        public override bool Equals(object obj)
        {
            return obj is ProductId other && Equals(other);
        }

        // 상품 식별자의 해시 값 반환
        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }
    }

    public readonly struct PurchaseRequest
    {
        // 구매 요청의 상품·요청 식별자와 Phase를 검증하고 저장
        public PurchaseRequest(ProductId productId, string purchaseRequestId, PhaseState phase)
        {
            // 저장할 상품 식별자 유효성 확인
            if (!productId.IsValid)
            {
                throw new ArgumentException("ProductId must be valid.", nameof(productId));
            }

            // 비어 있는 구매 요청 식별자 차단
            if (string.IsNullOrWhiteSpace(purchaseRequestId))
            {
                throw new ArgumentException("PurchaseRequestId must not be empty.", nameof(purchaseRequestId));
            }

            // Phase 계약에 정의된 값인지 확인
            if (!IsDefinedPhase(phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "PhaseState must be a defined value.");
            }

            ProductId = productId;
            PurchaseRequestId = purchaseRequestId;
            Phase = phase;
        }

        // 구매 대상 상품 식별자
        public ProductId ProductId { get; }

        // 중복 요청 판정에 사용할 구매 요청 식별자
        public string PurchaseRequestId { get; }

        // 구매 요청이 발생한 현재 Phase
        public PhaseState Phase { get; }

        // Phase 계약에 정의된 상태인지 확인
        private static bool IsDefinedPhase(PhaseState phase)
        {
            return phase == PhaseState.Preparation
                || phase == PhaseState.Combat
                || phase == PhaseState.StageClear;
        }
    }
}
