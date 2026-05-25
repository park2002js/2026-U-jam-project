using UnityEngine;

[CreateAssetMenu(fileName = "Element", menuName = "Scriptable Objects/Element")]
public class Element : ScriptableObject
{
    [TextArea]
    public string description = "여기에 독+바람 연계 시 일어날 효과 등의 데이터를 관리할 예정";
    // 추후 기획에 따라 연계 데미지 계수 등을 여기에 추가합니다.
}
