using UnityEngine;
using System.Collections.Generic;
using EnemySystem;

public class ElementReceiver : MonoBehaviour
{
    private Enemy enemyScript;
    private Collider enemyCollider; 

    private class ActiveElement
    {
        public Element data;
        public float timeLeft;
        public float dotTimer;
        public float customDotDmg = 0f; // 도트 증폭용
    }

    private List<ActiveElement> activeElements = new List<ActiveElement>();

    private void Awake()
    {
        enemyScript = GetComponent<Enemy>();
        enemyCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        for (int i = activeElements.Count - 1; i >= 0; i--)
        {
            ActiveElement active = activeElements[i];
            active.timeLeft -= Time.deltaTime;

            float currentDot = active.customDotDmg > 0 ? active.customDotDmg : active.data.damagePerSecond;

            if (currentDot > 0 && enemyScript != null)
            {
                active.dotTimer += Time.deltaTime;
                if (active.dotTimer >= 1f)
                {
                    enemyScript.TakeDamage(currentDot);
                    active.dotTimer -= 1f;
                }
            }
            if (active.timeLeft <= 0f) activeElements.RemoveAt(i);
        }
    }

    public void ApplyElement(DamageInfo info)
    {
        Element incomingElement = info.Element;
        if (incomingElement == null) return;

        if (incomingElement.baseEffectPrefab != null)
        {
            Vector3 effectPos = transform.position + new Vector3(0, 0.5f, 0); 
            GameObject effect = Instantiate(incomingElement.baseEffectPrefab, effectPos, Quaternion.identity);
            effect.transform.SetParent(this.transform); 
            AdjustEffectSize(effect); 
        }

        if (activeElements.Count >= 2) return; 

        activeElements.Add(new ActiveElement { data = incomingElement, timeLeft = incomingElement.duration, dotTimer = 0f });

        string slot1 = activeElements.Count > 0 ? activeElements[0].data.elementType.ToString() : "";
        string slot2 = activeElements.Count > 1 ? activeElements[1].data.elementType.ToString() : "";
        string listStatus = $"[{slot1} + {slot2}]";

        if (activeElements.Count == 2)
        {
            CheckAndTriggerCombo(listStatus);
        }
    }

    private void CheckAndTriggerCombo(string listStatus)
    {
        Element first = activeElements[0].data;
        Element second = activeElements[1].data;

        if (first.TryGetComboReaction(second.elementType, out ComboReaction reaction))
        {
            ExecuteComboByType(first, reaction, listStatus);
        }
        else if (second.TryGetComboReaction(first.elementType, out ComboReaction reverseReaction))
        {
            ExecuteComboByType(second, reverseReaction, listStatus);
        }
        else
        {
            activeElements.RemoveAt(0); 
        }
    }

    // ✨ [핵심 함수] SO의 콤보 타입에 따라 스위치를 켭니다!
    private void ExecuteComboByType(Element originalElement, ComboReaction reaction, string listStatus)
    {
        Debug.Log($"<color=orange>{listStatus} 연계 발동! 타입: {reaction.comboType}</color>");

        switch (reaction.comboType)
        {
            case ComboType.Instant:
                if (reaction.comboEffectPrefab != null) Instantiate(reaction.comboEffectPrefab, transform.position, Quaternion.identity);
                if (enemyScript != null) enemyScript.TakeDamage(reaction.comboDamage);
                break;

            case ComboType.DelayedAoE:
                // 적이 죽어도 폭발은 남아야 하므로, 허공에 빈 게임오브젝트를 만들고 범용 콤보 매니저를 붙입니다.
                GameObject handlerObj = new GameObject("DelayedAoE_Handler");
                handlerObj.transform.position = transform.position;
                
                // ✨ 이 부분을 ComboHandler로 변경했습니다!
                ComboHandler handler = handlerObj.AddComponent<ComboHandler>(); 
                
                handler.Setup(reaction); 
                break;

            case ComboType.DoT_Amplify:
                // 도트뎀 2배 증폭
                activeElements.Clear();
                activeElements.Add(new ActiveElement { data = originalElement, timeLeft = originalElement.duration, customDotDmg = originalElement.damagePerSecond * 2f });
                return; // 도트 증폭은 리스트를 다르게 갱신하므로 밑의 Clear를 무시하고 리턴

            case ComboType.ChainAttack:
                // 1. 화려한 100만 볼트 이펙트 생성
                if (reaction.comboEffectPrefab != null)
                {
                    Instantiate(reaction.comboEffectPrefab, transform.position, Quaternion.identity);
                }

                // 2. 인스펙터에 적은 어마어마한 범위(comboRadius)로 적들을 탐색
                Collider[] chainHits = Physics.OverlapSphere(transform.position, reaction.comboRadius, LayerMask.GetMask("Enemy"));
                int chainedCount = 0;

                foreach (Collider hit in chainHits)
                {
                    if (hit.gameObject == this.gameObject) continue; // 자기 자신은 제외

                    Enemy targetEnemy = hit.GetComponent<Enemy>();
                    if (targetEnemy != null)
                    {
                        // 3. 인스펙터에 적은 강력한 데미지(comboDamage)를 입힘
                        DamageInfo chainInfo = DamageInfo.Default(reaction.comboDamage, 0f, null);
                        DamageSystem.ApplyDamage(targetEnemy.gameObject, chainInfo);
                        
                        chainedCount++;
                        // 4. 인스펙터에 적은 최대 타겟 수(extraTargetCount)만큼만 튕김
                        if (chainedCount >= reaction.extraTargetCount) break;
                    }
                }
                break;
        }

        activeElements.Clear();
    }

    private void AdjustEffectSize(GameObject effectObj)
    {
        if (enemyCollider == null) return;
        float enemyVisualSize = Mathf.Max(enemyCollider.bounds.size.x, enemyCollider.bounds.size.y, enemyCollider.bounds.size.z);
        effectObj.transform.localScale = new Vector3(enemyVisualSize, enemyVisualSize, enemyVisualSize);
    }
}