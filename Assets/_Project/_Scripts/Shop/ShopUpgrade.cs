using System.Collections.Generic;
using UnityEngine;

namespace ItemShopSystem
{
    public class ShopUpgrade
    {
        private Dictionary<string, string> upgradeTable;


        public ShopUpgrade()
        {
            upgradeTable =
                new Dictionary<string, string>();

            // 임시 강화 데이터

            upgradeTable.Add(
                "Item_001",
                "Item_002"
            );

            upgradeTable.Add(
                "Item_002",
                "Item_003"
            );

            upgradeTable.Add(
                "Item_003",
                "Item_004"
            );
        }


        public string Upgrade(string itemId)
        {
            if (!upgradeTable.ContainsKey(itemId))
            {
                Debug.LogError(
                    $"[ShopUpgrade] 강화할 수 없는 아이템입니다 : {itemId}"
                );

                return null;
            }

            string upgradedItemId =
                upgradeTable[itemId];

            Debug.Log(
                $"[ShopUpgrade] 강화 성공 : " +
                $"{itemId} → {upgradedItemId}"
            );

            return upgradedItemId;
        }
    }
}