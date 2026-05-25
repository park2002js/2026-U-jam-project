using UnityEngine;

namespace Defense
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

        public virtual void Initialize(Vector2Int pos)
        {
            gridPosition = pos;
        }

        public bool CanUpgrade()
        {
            return currentLevel < maxLevel;
        }

        // 자식 클래스에서 반드시 구현해야 할 추상 함수
        public abstract string GetUpgradeDescription();
        public abstract int GetUpgradeCost();
        public abstract void ApplyUpgrade();

        protected void FreeTileOnDestroy()
        {
            // 향후 구현될 GridManager/TileManager 연동
            // if (GridManager.Instance != null)
            // {
            //     GridManager.Instance.FreeTile(gridPosition);
            //     Debug.Log($"{gridPosition} 타일의 점유가 해제되었습니다.");
            // }
        }

        protected virtual void OnDestroy()
        {
            FreeTileOnDestroy();
        }
    }
}