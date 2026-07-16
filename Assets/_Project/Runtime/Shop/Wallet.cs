using System;

namespace UJam.Runtime.Shop
{
    public sealed class Wallet
    {
        // 지갑 소유 변경 가능 잔액
        private long _balance;

        // 초기 잔액 기반 지갑 생성
        public Wallet(CurrencyAmount initialBalance)
        {
            // 생성 잔액 저장
            _balance = initialBalance.Value;
        }

        // 현재 잔액 조회
        public CurrencyAmount Balance
        {
            get
            {
                // 값 객체 잔액 반환
                return new CurrencyAmount(_balance);
            }
        }

        // 오버플로 없는 잔액 추가
        public bool TryCredit(CurrencyAmount amount)
        {
            // long 범위 초과 확인
            if (amount.Value > long.MaxValue - _balance)
            {
                // 오버플로 요청 차단
                return false;
            }

            // 검증 금액 반영
            _balance += amount.Value;

            // 입금 성공 반환
            return true;
        }

        // 잔액 부족 시 무변경 차감
        public bool TrySpend(CurrencyAmount amount)
        {
            // 잔액 부족 차감 차단
            if (amount.Value > _balance)
            {
                // 지출 요청 차단
                return false;
            }

            // 검증 금액 차감
            _balance -= amount.Value;

            // 지출 성공 반환
            return true;
        }
    }
}
