using UnityEngine;
using System.Collections;
using EnemySystem;

// SO 데이터(장판, 딜레이)를 전달받아 폭발만 시켜주고 사라지는 범용 핸들러입니다.
public class ComboHandler : MonoBehaviour
{
    private ComboReaction reactionData;

    public void Setup(ComboReaction data)
    {
        reactionData = data;
        StartCoroutine(ExecuteSequence());
    }

    private IEnumerator ExecuteSequence()
    {
        // ✨ 1. 적의 위치에서 아래로 레이저(Raycast)를 쏴서 정확한 '바닥' 좌표를 찾습니다.
        Vector3 groundPosition = transform.position;
        // 위에서 아래로 레이저를 쏴서 "Ground" 레이어에 닿은 곳을 바닥으로 인식!
        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 20f, LayerMask.GetMask("Ground")))
        {
            groundPosition = hit.point; 
        }

        GameObject indicator = null;

        if (reactionData.indicatorPrefab != null)
        {
            // ✨ 2. 찾은 바닥(groundPosition) 좌표에 장판을 깔아줍니다.
            indicator = Instantiate(reactionData.indicatorPrefab, groundPosition + new Vector3(0, 0.05f, 0), Quaternion.identity);
            
            // 지난번에 고친 넓이만 넓어지는 코드!
            indicator.transform.localScale = new Vector3(reactionData.comboRadius * 2f, indicator.transform.localScale.y, reactionData.comboRadius * 2f);
            
            Destroy(indicator, reactionData.delayTime - 0.1f);
        }

        yield return new WaitForSeconds(reactionData.delayTime);

        if (reactionData.comboEffectPrefab != null)
        {
            // ✨ 3. 폭발 이펙트도 적의 배꼽이 아니라 '바닥'에서부터 웅장하게 솟구치도록 변경!
            Instantiate(reactionData.comboEffectPrefab, groundPosition, Quaternion.identity);
        }

        // ✨ 4. 데미지 판정도 바닥을 기준으로 잡습니다. (RaycastHit의 hit 변수와 겹치지 않게 hitCol로 이름 변경)
        Collider[] hits = Physics.OverlapSphere(groundPosition, reactionData.comboRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider hitCol in hits)
        {
            Enemy targetEnemy = hitCol.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                DamageInfo info = DamageInfo.Default(reactionData.comboDamage, 0f, null);
                DamageSystem.ApplyDamage(targetEnemy.gameObject, info);
            }
        }

        Destroy(gameObject, 3f);
    }
}