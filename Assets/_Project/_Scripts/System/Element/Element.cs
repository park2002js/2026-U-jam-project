using UnityEngine;
using System.Collections.Generic;

public enum ElementType { None, Fire, Water, Wind, Lightning, Earth }

[System.Serializable]
public struct ComboReaction
{
    public ElementType incomingElement;
    public GameObject comboEffectPrefab;
    public float comboDamage;
    public float comboRadius;
}

[CreateAssetMenu(fileName = "Element", menuName = "Scriptable Objects/Element")]
public class Element : ScriptableObject
{
    [Header("기본 정보")]
    public ElementType elementType;
    public string description;
    
    // ✨ 1. [수정됨] 단일 속성이 묻었을 때 터질 기본 프리팹 추가!
    public GameObject baseEffectPrefab; 

    [Header("단일 상태이상 효과")]
    public float duration = 3f;
    public float damagePerSecond = 0f;
    public float speedMultiplier = 1f;
    public bool isStunned = false;

    [Header("특수 효과 (번개 등)")]
    public int chainCount = 0;              
    public float chainRadius = 5f;          
    public float chainDamageRatio = 0.5f;   

    [Header("속성 연계 (Combo) 설정")]
    public List<ComboReaction> comboReactions = new List<ComboReaction>();

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