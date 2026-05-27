using UnityEngine;
using System.Collections;
using EnemySystem;

public class ComboHandler : MonoBehaviour
{
    private ComboReaction comboData;

    public void Setup(ComboReaction data)
    {
        comboData = data;

        if (data.comboType == ComboType.DelayedAoE)
        {
            // ⚡ [Delayed AoE] 1단계: 경고장(Telegraph) 먼저 깔기
            if (data.indicatorPrefab != null)
            {
                // 기획자님이 장부에 넣은 Telegraph 프리팹을 띄우고 크기를 맞춥니다.
                GameObject warning = Instantiate(data.indicatorPrefab, transform.position, Quaternion.identity);
                // ✨ [수정됨] 높이(Y)는 원본의 납작한 두께를 유지하고, 넓이(X, Z)만 반경에 맞게 늘립니다!
                warning.transform.localScale = new Vector3(data.comboRadius, warning.transform.localScale.y, data.comboRadius);
                Destroy(warning, data.delayTime); // 터지기 직전에 경고장 삭제
            }
            else
            {
                // 빈칸이면 아까 만들었던 빨간 테두리를 그립니다.
                DrawRangeCircle();
            }

            // 시한폭탄 코루틴 시작! (N초 뒤에 터짐)
            StartCoroutine(DelayedExplosionRoutine());
        }
        else if (data.comboType == ComboType.AreaDoT)
        {
            // ✨ [수정됨] 엉뚱한 경고장(Telegraph) 띄우는 코드 완전 삭제!
            // 대신, 장판이 깔림과 동시에 "이 장판의 피격 범위는 여기까지다"라고 보여주는 경계선만 그립니다.
            DrawRangeCircle();

            // 🌋 2단계: 지속형 이펙트(용암, 전기장 등) 깔기
            if (data.comboEffectPrefab != null)
            {
                GameObject fx = Instantiate(data.comboEffectPrefab, transform.position, Quaternion.identity);
                float visualScale = data.comboRadius * 0.6f; 
                fx.transform.localScale = new Vector3(visualScale, fx.transform.localScale.y, visualScale);
                // 장판 이펙트도 설정한 시간(delayTime)만큼 유지 후 삭제
                Destroy(fx, data.delayTime > 0 ? data.delayTime : 1f); 
            }

            // 3단계: 지속 데미지 코루틴 시작!
            StartCoroutine(AreaDoTRoutine());
        }
    }

    // 💣 3초 뒤에 번개가 쾅! 떨어지는 코루틴
    private IEnumerator DelayedExplosionRoutine()
    {
        // 1. 장부에 적힌 Delay Time(3초)만큼 조용히 기다립니다.
        yield return new WaitForSeconds(comboData.delayTime);

        // 2. 시간이 다 되면 진짜 이펙트(Plexus AoE)를 소환!
        if (comboData.comboEffectPrefab != null)
        {
            GameObject fx = Instantiate(comboData.comboEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = new Vector3(comboData.comboRadius, comboData.comboRadius, comboData.comboRadius);
            Destroy(fx, 2f); // 번개 이펙트는 2초 뒤에 깔끔하게 삭제
        }

        // 3. 반경 안의 적들에게 데미지 쾅!
        Collider[] hits = Physics.OverlapSphere(transform.position, comboData.comboRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null) enemy.TakeDamage(comboData.comboDamage);
        }
        
        Destroy(gameObject); // 역할이 끝난 핸들러 삭제
    }

    // 🌋 밟고 있는 동안 계속 데미지를 주는 코루틴
    private IEnumerator AreaDoTRoutine()
    {
        float timer = 0f;
        while (timer < comboData.delayTime)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, comboData.comboRadius, LayerMask.GetMask("Enemy"));
            foreach (Collider hit in hits)
            {
                Enemy enemy = hit.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(comboData.comboDamage); 
                }
            }
            yield return new WaitForSeconds(1f);
            timer += 1f;
        }
        Destroy(gameObject);
    }

    // (기존 빨간 테두리 그리는 함수)
    private void DrawRangeCircle()
    {
        LineRenderer line = gameObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false; 
        line.startWidth = 0.05f;    
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = Color.red;
        line.endColor = Color.red;

        int segments = 50; 
        line.positionCount = segments + 1;
        float radius = comboData.comboRadius; 
        float angle = 0f;

        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            line.SetPosition(i, new Vector3(x, 0.1f, z)); 
            angle += (360f / segments);
        }
    }
}