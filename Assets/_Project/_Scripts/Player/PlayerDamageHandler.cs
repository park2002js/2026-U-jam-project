using System;
using UnityEngine;

// 외부 공격을 수신하여 매니저에게 전달하고 시각적 반응을 방송하는 어댑터 클래스
/// <summary>
/// [설계 원칙 기록] 
/// 1. 이 스크립트는 체력이나 무적 상태를 직접 연산/저장하지 않습니다. (SSOT 원칙 보존)
/// 2. 외부의 공격(TakeDamage)을 받아 PlayerStatManager에 심사(ApplyDamage)를 요청합니다.
/// 3. 심사가 통과되면, 미래에 구현될 VisualManager를 위해 C# Event 방송만 송출합니다.
/// </summary>
public class PlayerDamageHandler : MonoBehaviour, IDamageable
{
    // 추후 PlayerVFXManager, PlayerAnimator 등이 구독할 피격 방송 채널
    public event Action<DamageInfo> OnTakeDamageEvent;

    public void TakeDamage(DamageInfo info)
    {
        // GameEndManager로 부터 데미지(Damage) 잠금 확인
        if (PlayerStatManager.Instance.HasLock(PlayerLockFlags.Damage))
        {
            // 컷신, 게임 오버 등으로 데미지 판정이 잠겼으므로 공격 무시 (World Freeze)
            Debug.Log($"[PlayerDamageHandler] 데미지 판정이 잠겨있으므로 공격 무시됨 (정비 페이즈, 게임 오버 등).");
            return;
        }

        // 1. 무적 관통 및 갱신 검사 (수문장 로직)
        if (!info.BypassInvincibility && PlayerStatManager.Instance.IsInvincible)
        {
            // 무적 상태일 때 들어오는 일반 공격은 '완전한 회피/무시'로 간주합니다.
            // 구르기 등으로 회피한 공격의 피격 무적 시간(info.InvincibleTime)을 플레이어에게 부여하는 것은 모순이므로, 
            // 갱신 로직을 아예 타지 않고 즉시 반환(return)하여 공격을 소멸시킵니다.
            Debug.Log($"[PlayerDamageHandler] 무적 상태로 데미지({info.Amount}) 무시됨.");
            return;
        }

        // 2. 데미지 차감 요청 (PlayerStatManager에게 청구서 전달)
        bool damageApplied = PlayerStatManager.Instance.ApplyDamage(info.Amount);

        // 3. 체력이 실제로 깎였다면 피격 리액션 및 무적 처리
        if (damageApplied)
        {
            // TODO: [Tech Debt] 추후 info.Reaction 타입에 따른 애니메이션 캔슬 및 넉백 물리 연산 추가
            // TODO: [Tech Debt] 추후 info.Instigator 정보를 활용한 어그로 변경 로직 추가

            // 피격 무적 부여
            if (info.InvincibleTime > 0)
            {
                PlayerStatManager.Instance.SetInvincible(info.InvincibleTime);
            }

            // 4. 시각적 반응 이벤트 송출 (현재는 디버그 로그로 대체)
            OnTakeDamageEvent?.Invoke(info);
            
            Debug.Log($"[PlayerDamageHandler] 피격 성공! 데미지: {info.Amount}, " +
                      $"부여된 무적시간: {info.InvincibleTime}초, " +
                      $"현재 체력: {PlayerStatManager.Instance.CurrentHealth}");
        }
    }
}