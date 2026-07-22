using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Lightning : MonoBehaviour // ✨ 클래스 이름이 Lightning으로 맞춰졌습니다.
{
    public void Setup(Vector3 startPoint, Vector3 endPoint)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        
        // 발바닥이 아니라 몬스터의 몸통 중앙(배꼽)에서 연결되도록 높이를 살짝 올립니다.
        Vector3 offset = new Vector3(0, 0.5f, 0); 
        
        // 선의 시작점과 끝점을 설정
        lr.SetPosition(0, startPoint + offset);
        lr.SetPosition(1, endPoint + offset);
        
        // 0.2초 뒤에 번개 줄기가 팟! 하고 사라짐
        Destroy(gameObject, 0.2f);
    }
}