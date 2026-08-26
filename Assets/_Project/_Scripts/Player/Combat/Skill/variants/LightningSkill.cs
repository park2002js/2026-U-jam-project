using UnityEngine;
using System.Collections.Generic;
using UJam.Runtime.Enemy;
using UJam.Runtime.Combat;

namespace UJam.Runtime.Player
{
    /// <summary>
    /// 가장 기본적인 스킬이다.
    /// <para>스킬 사용시, 마우스 커서 위치에서 일정 범위 내에 있는 적들에게 피해를 입힌다.</para>
    /// 일정 범위 내에 있는 적들에 대해서는 Sphere의 Hitbox를 만들어서 Overlap 여부를 탐지한다.
    /// </summary>
    public class LightningSkill : PlayerSkill
    {
        [Header("Lightning Effect 정의")]
        public GameObject lightningBoltPrefab;  // Lightning 스크립트 붙은 번개 줄기 프리팹
        public float strikeHeight = 10f;        // 줄기 시작 하늘 높이
        public float boltWidthRatio = 0.1f;     // 줄기 굵기 = zoneRadius × 이 값
        public float boltLifetime = 0.15f;      // 줄기 유지 시간
        public float boltJagger = 0.3f;         // 줄기의 지그재그 정도 

        [Header("Ground Effect 정의")]
        public GameObject strikeEffectPrefab;  // GroundLight (땅 빛 이펙트)
        public float effectScale = 0.3f;       // 땅 이펙트 크기 배율
        public float effectLifetime = 1.5f;    // 땅 이펙트 유지 시간(초)

        [Header("효과 적용 정의")]
        public float zoneRadius = 1f;      // 원 크기 (조준 원 + 데미지 범위)
        public float zoneLifetime = 0.5f;  // 콜라이더 유지 시간(초). 이 시간 안에 들어온 적도 맞음
        public float damageMultiplier = 10f;  // 데미지 배율
        public float skillCoolTime = 10f; // 스킬 쿨 타임
        public Sprite skillIcon; // 스킬 아이콘

        
        // 이펙트 생성 및 Trigger 생성의 원점이 될 좌표
        // 마우스 커서가 가리키는 Ground 위 좌표를 PlayerSkillManager에서 전달한다.
        private Vector3 _targetPos;
        
        // combatManager 할당 적용 및 초기화
        public override void Init(PlayerCombatManager combatManager)
        {
            base.Init(combatManager);
            EffectRadius = zoneRadius;
            CastType = SkillCastType.Normal; // 일반 시전 정의
            CoolTime = skillCoolTime;
            SkillIcon = skillIcon;
        }

        // 단계별로 시행
        public override void Excute(Vector3 targetPosition)
        {
            _targetPos = targetPosition;

            // 1. 번개 줄기 생성 : 프리팹을 착탄점을 기준으로 하늘로 세워서 생성한다.
            // 즉, 착탄점에서 역으로 하늘로 향하는 것이 된다.
            MakeLightningBolt();

            // 2. 착탄 지점 이펙트 생성 : 땅에 떨어진 낙뢰의 잔류를 표현한다. 설정한 시간만큼 진행 후 종료된다.
            MakeLightningGround();

            // 3. 콜라이더에 들어온 적들에게 데미지를 입힘 : 콜라이더 생성 후 지정 시간 동안 들어온 모든 적에게 TakeDamage를 호출한다.
            TakeDamageForEnemy();
        }

        /// <summary>
        /// 낙뢰 이펙트 구현을 위한 함수
        /// </summary>
        private void MakeLightningBolt()
        {
            // 낙뢰 표현을 위해, Prefab을 복사한 Object를 생성
            GameObject bolt = Instantiate(lightningBoltPrefab, _targetPos, Quaternion.identity);

            var jagger = boltJagger;    // 지그재그 정도
            var startPoint = _targetPos;    // 볼트의 시작점 : 마우스 커서가 가리키는 땅 위 좌표
            var endPoint = startPoint + Vector3.up * strikeHeight; // 볼트의 끝점 : 시작점으로부터, 설정한 번개 높이까지

            // 1. 할당한 번개 줄기 표현을 위한 Prefab에서 LineRender를 가져옴
            LineRenderer lr = bolt.GetComponent<LineRenderer>();
            lr.useWorldSpace = true;                  // 월드 좌표로 위치 잡기
            lr.textureMode = LineTextureMode.Tile;    // 텍스처 늘어짐 방지 (반복)

            // 2. 하늘→바닥을 여러 점으로 나누고, 중간 점만 좌우로 흔들어 지그재그
            int segments = 8;
            lr.positionCount = segments + 1;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 point = Vector3.Lerp(startPoint, endPoint, t);
                if (i != 0 && i != segments)
                {
                    point.x += Random.Range(-jagger, jagger);
                    point.z += Random.Range(-jagger, jagger);
                }
                lr.SetPosition(i, point);
            }

            lr.startWidth = boltWidthRatio;
            lr.endWidth = boltWidthRatio;
            lr.numCapVertices = 2;

            Destroy(bolt, boltLifetime);
        }

        /// <summary>
        /// 낙뢰 이후 지면의 이펙트를 구현하는 함수
        /// </summary>
        private void MakeLightningGround()
        {
            if (strikeEffectPrefab != null)
            {
                GameObject fx = Instantiate(strikeEffectPrefab, _targetPos, Quaternion.identity);

                ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in systems)
                {
                    var main = ps.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
                fx.transform.localScale = Vector3.one * (zoneRadius * effectScale);

                Destroy(fx, effectLifetime);
            }
        }

        /// <summary>
        /// 마우스 커서 위치에 Spere 형태의 hitbox를 생성하고, 생성 당시 내부에 있던 적들에게 피해를 입히는 함수
        /// </summary>
        private void TakeDamageForEnemy()
        {
            // 별도의 Trigger 생성을 위한 객체를 보유하지 않으므로 Collider 생성을 하지 않음

            // 1. 다른 GameObject들의 Transform이 업데이트 되기 이전의 위치를 기준으로 Overlap을 검사할 수 있으므로, 
            // SyncTransforms를 통해 변경된 transform 위치를 물리 엔진에 즉시 반영
            Physics.SyncTransforms();

            // 2. Collider가 여러개인 적은 Overlap 이벤트가 여러번 발생할 수 있으므로, 중복 적에게 hit되는 것을 방지하기 위한 hash
            var damagedEnemies = new HashSet<EnemyBase>();

            // 3. 공통으로 사용할 
            DamageInfo damageInfo = new DamageInfo(_combatManager.PlayerStatus.AttackDamage * damageMultiplier, nameof(LightningSkill), DamageSourceKind.Player);

            // 4. Sphere를 만든 뒤에 Overlap된 적들을 저장
            Collider[] hits = Physics.OverlapSphere(_targetPos, EffectRadius, _combatManager.EnemyMask, QueryTriggerInteraction.Ignore);
            
            // 5. 저장된 적들에게 데미지를 입힘
            foreach (Collider hit in hits)
            {
                // Collider가 EnemyBase보다 아래에 있는 경우에도 사용될수 있도록 GetComponentInParent를 사용
                EnemyBase enemy = hit.GetComponentInParent<EnemyBase>(); 
                if (enemy == null || !damagedEnemies.Add(enemy)) continue; // 같은 적 중복 타격 방지

                /* 여기에 적의 스킬을 적용할 때 부여할 효과들을 정의할 수 있음 */
                TakeEffects();

                enemy.TakeDamage(damageInfo);
            }
        }
    }
}