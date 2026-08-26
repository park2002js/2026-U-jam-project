using UnityEngine;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// PlayerCombatSystem은 MonoBehaviour로, PlayerInputSystem가 신호를 보내면 그에 대응되는 동작을 취하도록 함수를 제공한다.
    /// Skill과 Shooter에서 공통적으로 사용하는 속성들을 Inspector를 통해 전달받는다.
    /// 그 외에 세부적인 Runtime 데이터들은 Skill, Shooter 내부에서 Inspector를 통해 정의하도록 한다.
    /// 
    /// 여기서의 핵심 역할은 Skill과 Shoot을 각각 배정된 곳으로 신호를 보내줌과 동시에,
    /// 각각의 곳에서 공통적으로 요구하는 것들을 보유하고 있는 곳이다.
    /// 
    /// Skill의 세부 동작이나 이런 것들은 잘 모른다.
    /// </summary>
    public class PlayerCombatManager : MonoBehaviour
    {
        [Header("공통")]
        [SerializeField] [Tooltip("플레이어가 바라보는 메인 카메라")]
        private Camera _aimCamera; 
        
        [SerializeField] [Tooltip("마우스 레이캐스트로 맞출 '바닥' 레이어")]
        private LayerMask _groundMask;
        
        [SerializeField] [Tooltip("데미지 판정할 '적' 레이어")]
        private LayerMask _enemyMask; 

        [SerializeField] [Tooltip("데미지를 입힐 때 사용할 플레이어의 스테이터스")]
        private PlayerStatus _playerStatus;


        [Header("Player Shooter 관련")]
        [SerializeField] [Tooltip("PlayerShooter 객체를 할당")] 
        private PlayerShooter _playerShooter;


        [Header("Player Skill 관련")]
        [SerializeField] [Tooltip("Skill 사용 및 적용 전반을 담당하는 매니저 객체")]
        private PlayerSkillManager _playerSkillManager;
        
        [SerializeField] [Tooltip("Skill 사용시 보일 프리뷰를 정의한 Prefab")]
        private GameObject _playerSkillPreviewPrefab;
        
        [SerializeField] [Tooltip("Shooter에서 사용할 투사체 발사 위치")]
        private Transform _bulletSpawnPoint;
        
        [SerializeField] [Tooltip("Shooter에서 발사할 시각적 투사체 Prefab")]
        private GameObject _bulletPrefab;
        

        public Camera AimCamera { get { return _aimCamera; } }
        public GameObject PlayerSkillPreviewPrefab { get { return _playerSkillPreviewPrefab; } }

        public LayerMask GroundMask { get { return _groundMask; } }
        public LayerMask EnemyMask { get { return _enemyMask; } }

        public Transform BulletSpawnPoint { get { return _bulletSpawnPoint; } }
        public GameObject BulletPrefab { get { return _bulletPrefab; } }
        public PlayerStatus PlayerStatus { get { return _playerStatus; } }

        private void Awake()
        {
            if (_playerShooter != null)
            {
                _playerShooter.Init(this);
            }

            if (_playerSkillManager != null)
            {
                _playerSkillManager.Init(this);
            }
        }
        /// <summary>
        /// Player의 마우스 클릭은 기본 공격과 연결된다.
        /// Shooter의 TryShoot을 호출하여 Shooter측에서 알아서 공격을 처리하도록 만든다.
        /// </summary>
        public void DefaultAttack()
        {
            if (_playerShooter != null)
            {
                _playerShooter.TryShoot();
            }
        }

        public void Skill1()
        {
            if (_playerSkillManager != null)
            {
                _playerSkillManager.TryUse(0);
            }
        }

        public void Skill2()
        {
            if (_playerSkillManager != null)
            {
                _playerSkillManager.TryUse(1);
            }
        }
    }
}
