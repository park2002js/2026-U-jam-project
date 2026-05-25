using UnityEngine;

// 🌟 1. 사전 (Enum): 기획자님이 게임에서 쓸 모든 스탯의 이름을 여기에 다 적어둡니다.
// 나중에 기획이 추가되면 여기에 쉼표(,) 찍고 이름만 계속 추가하시면 됩니다!
public enum StatType
{
    MoveSpeed,      // 이동 속도
    AttackPower,    // 공격력
    Armor,          // 방어력
    AttackCooldown, // 공격 속도(쿨타임)
    MaxHP,          // 최대 체력
    MissChance      // 공격 미스 확률 (수증기 기획용!)
}

// 🌟 2. 수신기 규격 (Interface): "이 수신기를 단 녀석은 무조건 아래 명령을 알아들어야 한다"는 규칙입니다.
public interface IStatReceiver
{
    // "어떤 스탯(type)을, 얼만큼(amount) 바꿀까요?"
    void ModifyStat(StatType type, float amount);
    void TakeDamage(float amount);
}