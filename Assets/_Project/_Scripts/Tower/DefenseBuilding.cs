using UnityEngine;
namespace Building
{
    public abstract class DefenseBuilding : MonoBehaviour
    {
        public enum ElementType { None, Water, Lightning, Poison, Wind }

        [Header("Base Settings")]
        public string buildingName;
        public ElementType myElement;
        public int baseCost;

        [Header("Level Settings")]
        protected int currentLevel = 1;
        protected int maxLevel = 3;
        
        protected Vector2Int gridPosition; // 설치된 타일 좌표

        // --- 외부 호출 가능 공통 API ---

        // 최초 설치 시 GridManager 등에 의해 호출
        public virtual void Initialize(Vector2Int pos)
        {
            gridPosition = pos;
        }

        // 상점 UI 등에서 강화 가능 여부 확인용
        public bool CanUpgrade()
        {
            return currentLevel < maxLevel;
        }

        // --- 자식 클래스에서 반드시 구현해야 할 추상 함수 ---

        public abstract string GetUpgradeDescription(); // 강화 설명 텍스트 반환
        public abstract int GetUpgradeCost();           // 현재 레벨 기준 강화 비용 계산
        public abstract void ApplyUpgrade();            // 실제 능력치 상승 로직

        // --- 공통 로직 ---

        protected void FreeTileOnDestroy()
        {
            // 향후 구현될 GridManager/TileManager 연동
            if (GridManager.Instance != null)
            {
                GridManager.Instance.FreeTile(gridPosition);
                Debug.Log($"{gridPosition} 타일의 점유가 해제되었습니다.");
            }
        }

        // 오브젝트 파괴 시(철거 포함) 공통적으로 타일 해제 실행
        protected virtual void OnDestroy()
        {
            FreeTileOnDestroy();
        }
    }
}

