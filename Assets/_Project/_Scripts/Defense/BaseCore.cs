using UnityEngine;
using UJam.Runtime.Combat;
using UJam.Runtime.Player;

namespace UJam.Runtime.Defense
{
    public sealed class BaseCore : MonoBehaviour, IDamageable
    {
        [SerializeField] private PlayerStatus _playerStatus;

        private void Awake()
        {
            if (_playerStatus == null)
            {
                _playerStatus = FindFirstObjectByType<PlayerStatus>();
            }
        }

        // 피해 판정은 하지 않고 받은 정보를 PlayerStatus에 그대로 전달한다.
        public float TakeDamage(DamageInfo info)
        {
            return _playerStatus != null ? _playerStatus.TakeDamage(info) : 0f;
        }
    }
}
