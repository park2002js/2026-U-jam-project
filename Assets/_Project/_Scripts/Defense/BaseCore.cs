using System;
using UnityEngine;
using UJam.Runtime.Combat;

namespace UJam.Runtime.Defense
{
    [RequireComponent(typeof(Health))]
    public sealed class BaseCore : MonoBehaviour, IDamageable
    {
        // 거점 체력을 관리할 Component
        [SerializeField] private Health _health;

        // 거점 파괴 시 게임 시간을 멈출지 여부
        [SerializeField] private bool _pauseOnGameOver = true;

        // 중복 게임 오버를 막을 현재 상태
        private bool _isGameOver;

        // 거점 파괴 뒤 UI와 게임 흐름이 구독할 이벤트
        public event Action GameOverTriggered;

        // 현재 게임 오버 상태
        public bool IsGameOver
        {
            get
            {
                // 현재 거점 파괴 상태 반환
                return _isGameOver;
            }
        }

        // 같은 GameObject의 Health 연결 보완
        private void Awake()
        {
            // Inspector 참조가 없으면 같은 GameObject에서 Health 확인
            if (_health == null)
            {
                _health = GetComponent<Health>();
            }
        }

        // 거점 활성화 시 사망 callback 연결
        private void OnEnable()
        {
            // 준비된 Health에만 callback 연결
            if (_health != null)
            {
                _health.Died -= HandleHealthDied;
                _health.Died += HandleHealthDied;
            }
        }

        // 거점 비활성화 시 사망 callback 해제
        private void OnDisable()
        {
            // 연결된 Health가 있을 때만 callback 해제
            if (_health != null)
            {
                _health.Died -= HandleHealthDied;
            }
        }

        // Enemy 피해를 거점 Health에 전달
        public float TakeDamage(DamageInfo info)
        {
            // Enemy가 아닌 피해와 게임 오버 뒤 피해 차단
            if (info.SourceKind != DamageSourceKind.Enemy || _isGameOver || _health == null)
            {
                // 적용하지 않은 피해량 반환
                return 0f;
            }

            // Health가 실제로 감소시킨 피해량 반환
            return _health.ApplyDamage(info.Damage);
        }

        // 거점 체력 소진을 게임 오버로 전환
        private void HandleHealthDied()
        {
            // 이미 처리한 게임 오버 차단
            if (_isGameOver)
            {
                // 중복 게임 오버 처리 없이 종료
                return;
            }

            // 게임 오버 상태 먼저 기록
            _isGameOver = true;

            // 외부 UI와 흐름에 거점 파괴 통지
            GameOverTriggered?.Invoke();

            // 설정된 게임 오버에서 전체 시간 정지
            if (_pauseOnGameOver)
            {
                Time.timeScale = 0f;
            }
        }
    }
}
