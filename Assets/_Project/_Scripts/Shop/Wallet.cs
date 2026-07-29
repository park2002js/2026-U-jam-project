using System;
using UnityEngine;

namespace UJam.Runtime.Shop
{
    public sealed class Wallet : MonoBehaviour
    {
        // Scene에서 사용할 단일 Wallet 인스턴스
        public static Wallet Instance { get; private set; }

        //쟈화 변경 전달용 함수
        public event Action<long> OnCurrencyChanged;

        // Inspector에서 설정할 시작 재화
        [SerializeField, Min(0)] private long _currency;

        // 외부 조회와 직접 변경에 사용할 현재 재화
        public long Currency
        {
            get
            {
                // 현재 음수가 아닌 재화 반환
                return _currency;
            }
            set
            {
                // 외부 설정값을 음수가 아닌 범위로 저장
                _currency = value < 0L ? 0L : value;
                
                //UIPlayer에 변경된 재화를 전달
                OnCurrencyChanged?.Invoke(_currency); 
            }
        }

        // Scene의 단일 Wallet 등록
        private void Awake()
        {
            // 다른 Wallet 인스턴스가 이미 있으면 중복 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                // 중복 인스턴스 초기화 종료
                return;
            }

            // 현재 Component를 단일 Wallet로 등록
            Instance = this;

            // Inspector 음수 재화를 0으로 보정
            if (_currency < 0L)
            {
                _currency = 0L;
            }
        }

        // 현재 Wallet 제거 시 Singleton 참조 해제
        private void OnDestroy()
        {
            // 등록된 자기 자신만 참조 해제
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Enemy 보상 같은 양의 재화 추가
        public bool AddCurrency(long amount)
        {
            // 양수가 아닌 추가 요청 차단
            if (amount <= 0L || _currency == long.MaxValue)
            {
                // 재화 추가 실패 반환
                return false;
            }

            // overflow 없이 더할 수 있는 최대 수량 계산
            long available = long.MaxValue - _currency;
            // 허용 범위만큼 현재 재화 증가
            _currency += amount > available ? available : amount;

            //UIPlayer에 변경된 재화를 전달
            OnCurrencyChanged?.Invoke(_currency);

            // 재화 추가 성공 반환
            return true;
        }

        // 향후 Shop에서 사용할 재화 차감
        public bool TrySpend(long amount)
        {
            // 양수가 아니거나 잔액보다 큰 요청 차단
            if (amount <= 0L || amount > _currency)
            {
                // 재화 차감 실패 반환
                return false;
            }

            // 검증된 비용 차감
            _currency -= amount;

            //UIPlayer에 변경된 재화를 전달
            OnCurrencyChanged?.Invoke(_currency);

            // 재화 차감 성공 반환
            return true;
        }
    }
}
