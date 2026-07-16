using System;

namespace UJam.Runtime.Shop
{
    public readonly struct CurrencyAmount : IEquatable<CurrencyAmount>
    {
        // 음수가 아닌 통화 금액을 검증하고 저장
        public CurrencyAmount(long value)
        {
            // 음수 통화 금액 차단
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "CurrencyAmount must not be negative.");
            }

            Value = value;
        }

        // 검증된 통화 금액
        public long Value { get; }

        // 통화 금액 값 비교
        public bool Equals(CurrencyAmount other)
        {
            return Value == other.Value;
        }

        // 객체가 CurrencyAmount인지 확인하고 값 비교
        public override bool Equals(object obj)
        {
            return obj is CurrencyAmount other && Equals(other);
        }

        // 통화 금액의 해시 값 반환
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    public enum PurchaseFailureReason
    {
        InsufficientFunds,
        ProductNotFound,
        InvalidPhase,
        InvalidRequest,
        DuplicateRequest
    }

    public sealed class PurchaseResult
    {
        // 구매 성공 또는 실패 상태를 저장
        private PurchaseResult(
            bool isSuccess,
            PurchaseOrder order,
            CurrencyAmount? chargedAmount,
            PurchaseFailureReason? failureReason)
        {
            IsSuccess = isSuccess;
            Order = order;
            ChargedAmount = chargedAmount;
            FailureReason = failureReason;
        }

        // 구매 성공 여부
        public bool IsSuccess { get; }

        // 성공 시 Placement에 전달할 주문
        public PurchaseOrder Order { get; }

        // 성공 시 차감된 통화 금액
        public CurrencyAmount? ChargedAmount { get; }

        // 실패 시 구매 실패 사유
        public PurchaseFailureReason? FailureReason { get; }

        // 유효한 주문을 포함한 성공 결과 생성
        public static PurchaseResult Succeeded(PurchaseOrder order, CurrencyAmount chargedAmount)
        {
            // 주문 없는 성공 결과 차단
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            return new PurchaseResult(true, order, chargedAmount, null);
        }

        // 실패 사유만 포함한 실패 결과 생성
        public static PurchaseResult Failed(PurchaseFailureReason failureReason)
        {
            // 승인되지 않은 실패 사유 차단
            if (!IsDefinedFailureReason(failureReason))
            {
                throw new ArgumentOutOfRangeException(nameof(failureReason), failureReason, "PurchaseFailureReason must be a defined value.");
            }

            return new PurchaseResult(false, null, null, failureReason);
        }

        // 승인된 실패 사유인지 확인
        private static bool IsDefinedFailureReason(PurchaseFailureReason failureReason)
        {
            return failureReason == PurchaseFailureReason.InsufficientFunds
                || failureReason == PurchaseFailureReason.ProductNotFound
                || failureReason == PurchaseFailureReason.InvalidPhase
                || failureReason == PurchaseFailureReason.InvalidRequest
                || failureReason == PurchaseFailureReason.DuplicateRequest;
        }
    }
}
