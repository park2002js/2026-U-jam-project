using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance { get; private set; } //battlemanager.Instance로 소환 가능하게 하는 싱글톤 패턴

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    [SerializeField] private int totalEnemies;
    [SerializeField] private int currentEnemies;

    public void SetTotalEnemies(int count)
    {
        totalEnemies = count; //총 적 수 설정
        currentEnemies = count; //현재 적 수도 총 적 수로 초기화
        Debug.Log($"전투 시작! 총 적 수: {totalEnemies}");
    }
}
