using System.Collections.Generic;
using UnityEngine;
using Defense; // ElementType이 있는 곳 (맞게 수정해 주세요)

// 🌟 1. 기획자님이 인스펙터에 적어 넣을 '상태이상 수치 보따리'
[System.Serializable]
public struct StatusData
{
    public string effectName;    // 연계 이름 (예: 대화재, 맹독)
    public float duration;       // 지속 시간 (예: 5초)
    public StatType targetStat;  // 바꿀 스탯 종류 (예: MoveSpeed)
    public float changeAmount;   // 스탯 변경 수치 (예: -2, 버프면 +)
    public float dotDamage;      // 초당 도트 데미지 (불 속성용!)
    public GameObject skillPrefab;
}

// 🌟 2. 누구랑 만났을 때 어떤 효과가 터질지 정의하는 연계 보따리
[System.Serializable]
public struct ComboData
{
    public ElementType meetElement;   // 만나는 속성 (예: Fire)
    public StatusData comboEffectData;// 연계가 터졌을 때 줄 상태이상 데이터!
}

// 🌟 3. 최종 스크립터블 오브젝트 (기획자님의 엑셀 시트 역할)
[CreateAssetMenu(fileName = "New Element", menuName = "Scriptable Objects/Element")]
public class Element : ScriptableObject
{
    public ElementType elementType; // 내 속성이 뭔지 (예: Fire)

    [Header("최초 부여 시 기본 효과")]
    public StatusData baseEffectData; // 예: 처음 불 맞았을 때 초당 5 데미지

    [Header("연계 반응 리스트")]
    public List<ComboData> combos; // 예: 불이 불을 만났을 때의 데이터 리스트

    // 들어온 속성이 내 연계 리스트에 있는지 확인해주는 함수
    public ComboData? CheckCombo(ElementType incomingType)
    {
        foreach (var combo in combos)
        {
            if (combo.meetElement == incomingType)
                return combo;
        }
        return null;
    }
}