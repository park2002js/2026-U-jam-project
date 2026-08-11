using System.Collections.Generic;
using UnityEngine;
using EnemySystem;

public class LightningDamageZone : MonoBehaviour
{
    private float damage;
    private LayerMask enemyMask;
    private readonly HashSet<Enemy> hitOnce = new HashSet<Enemy>();

    // PlayerSkillLightning에서 호출해 초기화
    public void Setup(float damageAmount, float radius, float lifetime, LayerMask enemyLayers)
    {
        damage = damageAmount;
        enemyMask = enemyLayers;

        // 트리거 콜라이더 세팅
        SphereCollider col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = radius;

        // 트리거 이벤트가 확실히 발동하도록 kinematic Rigidbody 부착
        Rigidbody rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // 생성 순간 '이미' 범위 안에 있던 적도 한 번 때림
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyMask);
        foreach (Collider c in hits) TryDamage(c);

        // lifetime 동안 유지 → 그 사이 걸어 들어온 적은 OnTriggerEnter로 처리
        if (lifetime > 0f) Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // enemyMask 레이어에 속한 것만 반응
        if ((enemyMask.value & (1 << other.gameObject.layer)) == 0) return;
        TryDamage(other);
    }

    private void TryDamage(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null || hitOnce.Contains(enemy)) return; // 같은 적 중복 타격 방지
        hitOnce.Add(enemy);
        enemy.TakeDamage(damage);
    }
}