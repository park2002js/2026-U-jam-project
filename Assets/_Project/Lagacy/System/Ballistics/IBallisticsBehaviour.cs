using UnityEngine;

namespace Ballistics
{
    // 탄환 발사 로직들이 반드시 지켜야 할 공통 규격
    public interface IBallisticsBehaviour
    {
        /// <summary>
        /// 무기(Ranged)에서 산출한 데이터를 바탕으로 실제 발사를 수행합니다.
        /// </summary>
        void Execute(Transform firePoint, Vector3 direction, float damage, float projectileSpeed, GameObject projectilePrefab, Element element = null);
    }
}