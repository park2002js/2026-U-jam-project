using UnityEngine;
using System.Collections.Generic;

public enum ElementType { None, Fire, Water, Earth, Wind, Lightning }

// ✨ [핵심] 콤보의 종류를 인스펙터에서 드롭다운으로 고를 수 있게 만듭니다!
public enum ComboType 
{ 
    Instant,       // 즉발기 (기본 데미지 쾅!)
    DelayedAoE,    // 텀 공격 / 장판기 (예고 후 폭발)
    DoT_Amplify,   // 도트딜 증폭 (불+불)
    ChainAttack,    // 연쇄 공격 (번개+번개)
    AreaDoT,
}

[System.Serializable]
public struct ComboReaction
{
    public ElementType incomingElement;
    
    [Header("Combo Type Settings")]
    public ComboType comboType; // ✨ 여기서 콤보 방식을 고릅니다.

    [Header("Visuals")]
    public GameObject indicatorPrefab;   // 장판/예고용 이펙트 (DelayedAoE 전용)
    public GameObject comboEffectPrefab; // 실제 폭발/타격 이펙트

    [Header("Stats")]
    public float comboDamage; // 데미지
    public float comboRadius; // 범위 (장판 크기 or 연쇄 반경)
    public float delayTime;   // 폭발까지 걸리는 시간 (DelayedAoE 전용)
    public int extraTargetCount; // 추가 타겟 수 (ChainAttack 전용)
}

[CreateAssetMenu(fileName = "New Element", menuName = "Defenses/Element")]
public class Element : ScriptableObject
{
    public ElementType elementType;
    public float damagePerSecond;
    public float duration;
    public GameObject baseEffectPrefab;

    [Header("Lightning Specific (Base)")]
    public int chainCount;
    public float chainRadius;
    public float chainDamageRatio;
    public GameObject chainBeamPrefab;

    [Header("Combo Reactions")]
    public List<ComboReaction> comboReactions;

    public bool TryGetComboReaction(ElementType incoming, out ComboReaction reaction)
    {
        foreach (var r in comboReactions)
        {
            if (r.incomingElement == incoming)
            {
                reaction = r;
                return true;
            }
        }
        reaction = default;
        return false;
    }
}