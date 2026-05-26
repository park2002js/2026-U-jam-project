using UnityEngine;
using System.Collections.Generic;
using EnemySystem;

// ✨ 문제의 'namespace Defense' 울타리를 제거했습니다!
public class ElementReceiver : MonoBehaviour
{
    private Enemy enemyScript;
    private Collider enemyCollider; 

    private class ActiveElement
    {
        public Element data;
        public float timeLeft;
        public float dotTimer;
    }

    private List<ActiveElement> activeElements = new List<ActiveElement>();

    private void Awake()
    {
        enemyScript = GetComponent<Enemy>();
        
        enemyCollider = GetComponent<Collider>();
        if (enemyCollider == null)
        {
            Debug.LogWarning($"[{gameObject.name}]에게 Collider가 없습니다! 이펙트 크기 조절이 안 됩니다.");
        }
    }

    private void Update()
    {
        for (int i = activeElements.Count - 1; i >= 0; i--)
        {
            ActiveElement active = activeElements[i];
            active.timeLeft -= Time.deltaTime;

            if (active.data.damagePerSecond > 0 && enemyScript != null)
            {
                active.dotTimer += Time.deltaTime;
                if (active.dotTimer >= 1f)
                {
                    enemyScript.TakeDamage(active.data.damagePerSecond);
                    active.dotTimer -= 1f;

                    Debug.Log($"<color=red>♨️ [{active.data.elementType}] 도트 데미지 {active.data.damagePerSecond} 틱 적용됨!</color>");
                }
            }

            if (active.timeLeft <= 0f)
            {
                activeElements.RemoveAt(i);
            }
        }
    }

    public void ApplyElement(DamageInfo info)
    {
        Element incomingElement = info.Element;
        if (incomingElement == null) return;

        if (incomingElement.elementType == ElementType.Lightning && incomingElement.chainCount > 0)
        {
            ExecuteChainLightning(info, incomingElement);
        }

        if (incomingElement.baseEffectPrefab != null)
        {
            Vector3 effectPos = transform.position + new Vector3(0, 0.5f, 0); 
            GameObject effect = Instantiate(incomingElement.baseEffectPrefab, effectPos, Quaternion.identity);
            
            effect.transform.SetParent(this.transform); 
            AdjustEffectSize(effect); 
        }

        if (activeElements.Count >= 2) return; 

        activeElements.Add(new ActiveElement 
        { 
            data = incomingElement, 
            timeLeft = incomingElement.duration, 
            dotTimer = 0f 
        });

        string slot1 = activeElements.Count > 0 ? activeElements[0].data.elementType.ToString() : "";
        string slot2 = activeElements.Count > 1 ? activeElements[1].data.elementType.ToString() : "";
        string listStatus = $"[{slot1} + {slot2}]";

        if (activeElements.Count == 1)
        {
            string dotMsg = incomingElement.damagePerSecond > 0 ? $" -> {incomingElement.damagePerSecond} 도트뎀 부여" : "";
            Debug.Log($"<color=cyan>{listStatus} {incomingElement.elementType} 속성이 부여됨{dotMsg}</color>");
        }
        else if (activeElements.Count == 2)
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
            TriggerCombo(reaction, listStatus);
        }
        else if (second.TryGetComboReaction(first.elementType, out ComboReaction reverseReaction))
        {
            TriggerCombo(reverseReaction, listStatus);
        }
        else
        {
            Debug.Log($"<color=grey>{listStatus} 속성 연계 실패! 첫 번째 속성({first.elementType}) 제거됨.</color>");
            activeElements.RemoveAt(0); 
        }
    }

    private void TriggerCombo(ComboReaction reaction, string listStatus)
    {
        string dmgMsg = reaction.comboDamage > 0 ? $" -> {reaction.comboDamage} 즉발 콤보 데미지!" : "";
        Debug.Log($"<color=orange>{listStatus} 속성 연계 발동!{dmgMsg}</color>");
        
        if (reaction.comboEffectPrefab != null)
        {
            Vector3 comboPos = transform.position + new Vector3(0, 0.5f, 0); 
            GameObject comboEffect = Instantiate(reaction.comboEffectPrefab, comboPos, Quaternion.identity);
            
            comboEffect.transform.SetParent(this.transform);
            AdjustEffectSize(comboEffect); 
        }

        if (enemyScript != null && reaction.comboDamage > 0)
            enemyScript.TakeDamage(reaction.comboDamage);

        activeElements.Clear();
    }

    private void AdjustEffectSize(GameObject effectObj)
    {
        if (enemyCollider == null) return;

        float enemyVisualSize = Mathf.Max(enemyCollider.bounds.size.x, enemyCollider.bounds.size.y, enemyCollider.bounds.size.z);
        effectObj.transform.localScale = new Vector3(enemyVisualSize, enemyVisualSize, enemyVisualSize);
    }

    private void ExecuteChainLightning(DamageInfo originalInfo, Element lightningData)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, lightningData.chainRadius, LayerMask.GetMask("Enemy"));
        int chainedCount = 0;
        float chainDamage = originalInfo.Amount * lightningData.chainDamageRatio;

        foreach (Collider hitCol in hits)
        {
            if (hitCol.gameObject == this.gameObject) continue;

            Enemy targetEnemy = hitCol.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(chainDamage);
                chainedCount++;
                if (chainedCount >= lightningData.chainCount) break;
            }
        }
    }
}