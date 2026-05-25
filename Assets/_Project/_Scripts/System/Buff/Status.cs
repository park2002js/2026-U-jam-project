// UniversalStatus.cs (기획자님이 원하신 궁극의 만능 상태이상 파일!)
using UnityEngine;

// 타겟이 누군지(적? 타워?) 상관없습니다. '수신기(IStatReceiver)'만 있으면 무조건 작동!
public class UniversalStatus : IStatusEffect<IStatReceiver>
{
    public string EffectName { get; private set; }
    public bool IsFinished { get; private set; }
    
    private float duration;
    
    // 🌟 이 버프가 어떤 스탯을, 얼만큼 바꿀지 담는 변수
    private StatType targetStat;
    private float changeAmount;
    private float dotDamagePerSec;

    public UniversalStatus(string name, float duration, StatType stat, float amount, float dotDamage = 0f)
    {
        EffectName = name;
        this.duration = duration;
        targetStat = stat;
        changeAmount = amount;
        dotDamagePerSec = dotDamage;
        IsFinished = false;
    }

    public void OnApply(IStatReceiver target)
    {
        // 🌟 타겟이 타워든 적이든 묻지도 따지지도 않고 명령을 내립니다!
        target.ModifyStat(targetStat, changeAmount);
        Debug.Log($"[{EffectName}] 발동: {targetStat} 스탯이 {changeAmount} 만큼 변경됨.");
    }

    public void OnTick(IStatReceiver target, float deltaTime)
    {
        duration -= deltaTime;
        if (dotDamagePerSec > 0)
        {
            target.TakeDamage(dotDamagePerSec * deltaTime);
        }

        if (duration <= 0) IsFinished = true;
    }

    public void OnRemove(IStatReceiver target)
    {
        // 🌟 끝날 때는 부호를 반대로(-) 해서 원상복구 시킵니다.
        if (changeAmount != 0) target.ModifyStat(targetStat, -changeAmount);
    }
}