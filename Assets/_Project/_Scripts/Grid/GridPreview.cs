using UnityEngine;

namespace UJam.Runtime.Grid
{
    // GameManager의 Grid 설정과 같은 값을 사용해 Scene에서 격자를 미리 표시, Grid System과 GameManager와 완전히 격리됨
    // Inspector 참고용에 가까움
    public sealed class GridPreview : MonoBehaviour
    {
        // Grid Cell 가로와 세로 크기
        [SerializeField, Min(0.0001f)] private float _gridCellSize = 1f;

        // Grid 시작 월드 좌표
        [SerializeField] private Vector3 _gridOrigin;

        // Grid 가로 Cell 수
        [SerializeField, Min(1)] private int _gridWidth = 10;

        // Grid 세로 Cell 수
        [SerializeField, Min(1)] private int _gridHeight = 10;

        // 선택된 오브젝트의 Grid 설정을 Scene에 선으로 표시
        private void OnDrawGizmosSelected()
        {
            // 잘못된 크기나 개수면 미리보기 중단
            if (_gridCellSize <= 0f || _gridWidth <= 0 || _gridHeight <= 0)
            {
                return;
            }

            // 다른 Gizmo에 돌려줄 기존 색상
            Color previousColor = Gizmos.color;
            Gizmos.color = Color.cyan;

            // 첫 Cell 중심에서 반 칸 뺀 Grid 왼쪽 경계
            float minX = _gridOrigin.x - _gridCellSize * 0.5f;
            // 첫 Cell 중심에서 반 칸 뺀 Grid 아래쪽 경계
            float minZ = _gridOrigin.z - _gridCellSize * 0.5f;
            // 전체 가로 길이를 더한 Grid 오른쪽 경계
            float maxX = minX + _gridWidth * _gridCellSize;
            // 전체 세로 길이를 더한 Grid 위쪽 경계
            float maxZ = minZ + _gridHeight * _gridCellSize;

            // 모든 세로 경계선을 순서대로 표시
            for (int column = 0; column <= _gridWidth; column += 1)
            {
                // 현재 세로선의 월드 X 좌표
                float x = minX + column * _gridCellSize;
                Gizmos.DrawLine(
                    new Vector3(x, _gridOrigin.y, minZ),
                    new Vector3(x, _gridOrigin.y, maxZ));
            }

            // 모든 가로 경계선을 순서대로 표시
            for (int row = 0; row <= _gridHeight; row += 1)
            {
                // 현재 가로선의 월드 Z 좌표
                float z = minZ + row * _gridCellSize;
                Gizmos.DrawLine(
                    new Vector3(minX, _gridOrigin.y, z),
                    new Vector3(maxX, _gridOrigin.y, z));
            }

            // Cell 테두리에 사용할 한 칸 크기
            Vector3 cellSize = new Vector3(_gridCellSize, 0f, _gridCellSize);

            // Grid 원점인 0, 0 Cell을 검은색 테두리로 표시
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(_gridOrigin, cellSize);

            // Width와 Height 개수 안에서 대각선 끝에 있는 마지막 Cell 중심
            Vector3 endCellCenter = new Vector3(
                _gridOrigin.x + (_gridWidth - 1) * _gridCellSize,
                _gridOrigin.y,
                _gridOrigin.z + (_gridHeight - 1) * _gridCellSize);
            // 시작과 끝이 같은 한 칸 Grid에서 두 색을 함께 보여줄 끝 테두리 크기
            Vector3 endCellSize = cellSize;

            // 한 칸 Grid면 빨간 테두리를 안쪽에 표시
            if (_gridWidth == 1 && _gridHeight == 1)
            {
                endCellSize *= 0.75f;
            }

            // Grid 대각선 끝 Cell을 빨간색 테두리로 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(endCellCenter, endCellSize);

            Gizmos.color = previousColor;
        }
    }
}
