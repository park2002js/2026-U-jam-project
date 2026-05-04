using UnityEngine;
using Equipment.Weapon; // 무기 네임스페이스

public class PlayerEquipments : MonoBehaviour
{
    [Header("Equipped Items")]
    [Tooltip("현재 장착 중인 무기")]
    [SerializeField] private Weapon currentWeapon;
    
    // TODO: 추후 Armor currentArmor 등 다른 장비 슬롯 확장 가능

    // 외부(PlayerController 등)에서 공격 명령을 내릴 때 호출하는 API
    public void ExecuteAttack()
    {
        // 장착된 무기가 있다면 무기 시스템의 자체 쿨타임 및 공격 로직을 실행
        if (currentWeapon != null)
        {
            currentWeapon.Attack();
        }
    }

    // void Start()
    // {
    //     EquipWeapon(currentWeapon);
    // }

    // 정비 페이즈 매니저나 상점/인벤토리 시스템에서 무기를 장착시킬 때 호출하는 API
    public void EquipWeapon(Weapon newWeapon)
    {
        currentWeapon = newWeapon;
        
        // 새로운 무기 장착 시, 해당 무기의 최종 스탯 1회 갱신 요청
        if (currentWeapon != null)
        {
            currentWeapon.UpdateFinalStats();
            Debug.Log($"[PlayerEquipments] 무기 장착 완료: {currentWeapon.gameObject.name}");
        }
    }
}