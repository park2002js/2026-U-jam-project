using UnityEngine;
using System.Collections.Generic;
using EnemySystem;
using UnityEngine.InputSystem;

public class ElementReceiver : MonoBehaviour
{
    private Enemy enemyScript;
    private Collider enemyCollider; 
    public Element[] testElements;

    [Header("Debug")]
    public bool isDummy = false;    // 샌드백 모드 스위치
    private Vector3 lockedPos;      // 처음 태어난 위치를 기억할 변수
    


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
        // ✨ [추가] 태어난 위치를 기억합니다.
        lockedPos = transform.position;
    }
    private void LateUpdate()
    {
        if (isDummy)
        {
            transform.position = lockedPos;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame) ApplyDebugElement(0); 
            if (Keyboard.current.digit2Key.wasPressedThisFrame) ApplyDebugElement(1); 
            if (Keyboard.current.digit3Key.wasPressedThisFrame) ApplyDebugElement(2); 
            if (Keyboard.current.digit4Key.wasPressedThisFrame) ApplyDebugElement(3); 
            if (Keyboard.current.digit5Key.wasPressedThisFrame) ApplyDebugElement(4); 
        }
        
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
    private void ApplyDebugElement(int index)
    {
        // 바구니가 비어있거나 장부가 없으면 무시
        if (testElements == null || index >= testElements.Length || testElements[index] == null) return;

        // 10의 깡딜과 함께 장부(속성)를 포장해서 '나 자신'에게 투하!
        DamageInfo debugInfo = DamageInfo.Default(10f, 0f, testElements[index]);
        DamageSystem.ApplyDamage(this.gameObject, debugInfo);
        
        Debug.Log($"[테스트] {gameObject.name}에게 {testElements[index].name} 속성 쾅!");
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

        // ✨ [추가됨] 평타인데 연쇄 번개(Chain Count) 설정이 있다면 지그재그 번개 발사
        if (incomingElement.chainCount > 0)
        {
            ExecuteChainLightning(info, incomingElement);
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
            case ComboType.AreaDoT:
                GameObject handlerObj = new GameObject("DelayedAoE_Handler");
                handlerObj.transform.position = transform.position + new Vector3(0, -0.5f, 0);
                ComboHandler handler = handlerObj.AddComponent<ComboHandler>(); 
                handler.Setup(reaction); 
                break;

            case ComboType.DoT_Amplify:
                activeElements.Clear();
                activeElements.Add(new ActiveElement { data = originalElement, timeLeft = originalElement.duration, customDotDmg = originalElement.damagePerSecond * 2f });
                return; 

            case ComboType.ChainAttack:
                if (reaction.comboEffectPrefab != null)
                {
                    Instantiate(reaction.comboEffectPrefab, transform.position, Quaternion.identity);
                }

                Collider[] chainHits = Physics.OverlapSphere(transform.position, reaction.comboRadius, LayerMask.GetMask("Enemy"));
                int chainedCount = 0;

                foreach (Collider hit in chainHits)
                {
                    if (hit.gameObject == this.gameObject) continue; 

                    Enemy targetEnemy = hit.GetComponent<Enemy>();
                    if (targetEnemy != null)
                    {
                        DamageInfo chainInfo = DamageInfo.Default(reaction.comboDamage, 0f, originalElement); 
                        DamageSystem.ApplyDamage(targetEnemy.gameObject, chainInfo);
                        chainedCount++;
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

    // ✨ [추가됨] 평타 체인 라이트닝 + 번개 줄기(Lightning) 생성 로직
    private void ExecuteChainLightning(DamageInfo originalInfo, Element lightningData)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, lightningData.chainRadius, LayerMask.GetMask("Enemy"));
        int chainedCount = 0;
        float chainDamage = originalInfo.Amount * lightningData.chainDamageRatio;

        Vector3 currentStartPos = transform.position;

        foreach (Collider hitCol in hits)
        {
            if (hitCol.gameObject == this.gameObject) continue;

            Enemy targetEnemy = hitCol.GetComponent<Enemy>();
            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage(chainDamage);

                // 인스펙터에 번개 줄기 프리팹이 등록되어 있다면 생성
                if (lightningData.chainBeamPrefab != null)
                {
                    GameObject beamObj = Instantiate(lightningData.chainBeamPrefab, currentStartPos, Quaternion.identity);
                    Lightning beamScript = beamObj.GetComponent<Lightning>();
                    if (beamScript != null)
                    {
                        beamScript.Setup(currentStartPos, targetEnemy.transform.position);
                    }
                }

                currentStartPos = targetEnemy.transform.position;

                chainedCount++;
                if (chainedCount >= lightningData.chainCount) break;
            }
        }
    }
}