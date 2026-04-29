using UnityEngine;
using System;
using EnemySystem;

namespace Utility
{
    public class DetectionSphere : MonoBehaviour
    {
        // 추격용 범위인지, 공격용 범위인지 구분하기 위한 이름표
        public enum RangeType { Chase, Attack }
        public RangeType type;

        // 나를 소환한 주인(적)이 누구인지 저장
        private Enemy owner;

        // 무언가 원 안에 들어오거나 나갈 때 주인에게 보낼 "문자 메시지" 같은 기능 (이벤트)
        public Action<Transform, RangeType> OnTargetEnter;
        public Action<Transform, RangeType> OnTargetExit;

        // 센서 초기 설정 (주인이 누구인지, 반지름이 얼마인지)
        public void Init(Enemy owner, float radius)
        {
            this.owner = owner;

            // 코드로 직접 레이어를 "EnemySensor"로 설정
            gameObject.layer = LayerMask.NameToLayer("EnemySensor");

            // 물리 충돌을 감지할 구체(Sphere) 컴포넌트를 가져오거나 없으면 추가함
            SphereCollider col = GetComponent<SphereCollider>();
            if (col == null) col = gameObject.AddComponent<SphereCollider>();

            // 물체가 부딪혀 튕겨나가지 않고 그냥 통과하게 만듦 (센서 역할)
            col.isTrigger = true;
            col.radius = radius;
        }

        // 센서 영역(Trigger) 안에 무언가 들어왔을 때 실행됨
        private void OnTriggerEnter(Collider other)
        {
            // "주인님! [무언가]가 [어떤 범위]에 들어왔어요!"라고 신호를 보냄
            OnTargetEnter?.Invoke(other.transform, type);
        }

        // 센서 영역 밖으로 무언가 나갔을 때 실행됨
        private void OnTriggerExit(Collider other)
        {
            // "주인님! [무언가]가 범위를 벗어났어요!"라고 알림
            OnTargetExit?.Invoke(other.transform, type);
        }
    }
}