using UnityEngine;
using System.Collections;
using EnemySystem;

public class ComboHandler : MonoBehaviour
{
    private ComboReaction comboData;

    public void Setup(ComboReaction data)
    {
        comboData = data;

        // 이펙트 생성 (장판 유지 시간 또는 폭발 대기 시간만큼 유지 후 삭제)
        if (data.comboEffectPrefab != null)
        {
            GameObject fx = Instantiate(data.comboEffectPrefab, transform.position, Quaternion.identity);
            float fxDuration = data.delayTime > 0 ? data.delayTime : 1f;
            Destroy(fx, fxDuration); 
        }

        // SO 장부의 타입에 따라 다른 코루틴 실행!
        if (data.comboType == ComboType.AreaDoT)
        {
            StartCoroutine(AreaDoTRoutine());
        }
        else // DelayedAoE (기존 시한폭탄)
        {
            StartCoroutine(DelayedExplosionRoutine());
        }
    }

    // 💣 [기존 기능] N초 뒤에 펑 터지는 시한폭탄
    private IEnumerator DelayedExplosionRoutine()
    {
        yield return new WaitForSeconds(comboData.delayTime);

        Collider[] hits = Physics.OverlapSphere(transform.position, comboData.comboRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(comboData.comboDamage);
        }
        Destroy(gameObject);
    }

    // 🌋 [새로운 기능] 바닥에 남아서 1초마다 틱 데미지를 주는 늪/전기장
    private IEnumerator AreaDoTRoutine()
    {
        float timer = 0f;
        
        // delayTime을 '장판 유지 시간'으로 사용합니다.
        while (timer < comboData.delayTime)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, comboData.comboRadius, LayerMask.GetMask("Enemy"));
            foreach (Collider hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    // 장부에 적힌 ComboDamage만큼 데미지를 줌
                    enemy.TakeDamage(comboData.comboDamage); 
                }
            }
            
            // 1초 대기 (틱 간격 조정 원하시면 이 숫자를 바꾸시면 됩니다!)
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }
        Destroy(gameObject);
    }
}