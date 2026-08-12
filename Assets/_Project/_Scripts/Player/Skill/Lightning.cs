using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Lightning : MonoBehaviour
{
    // 두 점을 잇는 기본 버전 (기존 연쇄번개 호환용)
    public void Setup(Vector3 startPoint, Vector3 endPoint)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        Vector3 offset = new Vector3(0, 0.5f, 0);
        lr.SetPosition(0, startPoint + offset);
        lr.SetPosition(1, endPoint + offset);
        Destroy(gameObject, 0.2f);
    }

    // 낙뢰용: 지그재그 형태 + 굵기 + 수명 지정
    public void Setup(Vector3 startPoint, Vector3 endPoint, float width, float lifetime, float jagger = 0.3f)
    {
        LineRenderer lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;                  // 월드 좌표로 위치 잡기
        lr.textureMode = LineTextureMode.Tile;    // 텍스처 늘어짐 방지 (반복)

        // 하늘→바닥을 여러 점으로 나누고, 중간 점만 좌우로 흔들어 지그재그
        int segments = 8;
        lr.positionCount = segments + 1;
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 point = Vector3.Lerp(startPoint, endPoint, t);
            if (i != 0 && i != segments)
            {
                point.x += Random.Range(-jagger, jagger);
                point.z += Random.Range(-jagger, jagger);
            }
            lr.SetPosition(i, point);
        }

        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCapVertices = 2;

        Destroy(gameObject, lifetime);
    }
}