using UnityEngine;

public class EnemyTest : MonoBehaviour
{
    [Header("이동 설정")]
    public float moveSpeed = 5f; // 적의 이동 속도

    void Update()
    {
        // 매 프레임마다 월드 좌표 기준 -Z 방향으로 이동
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime, Space.World);
    }

    // 터렛의 Raycast에 맞았을 때 호출될 함수
    public void TakeHit()
    {
        // 피격 시 파티클 효과나 사운드를 여기에 추가할 수 있습니다.
        Debug.Log($"{gameObject.name} 처치 완료!");
        
        // 즉시 오브젝트 파괴
        Destroy(gameObject);
    }
}