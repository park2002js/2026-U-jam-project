using System.Collections.Generic;
using UnityEngine;

namespace UJam.Runtime.Shop
{
    public class ShopFusion
    {
        private Dictionary<string, string> fusionRecipes;


        public ShopFusion()
        {
            fusionRecipes = new Dictionary<string, string>();

            // 임시 조합식

            fusionRecipes.Add(
                CreateRecipeKey("Item_001", "Item_002"),
                "Item_003"
            );

            fusionRecipes.Add(
                CreateRecipeKey("Item_003", "Item_004"),
                "Item_005"
            );
        }


        public string Fuse(
            string firstItemId,
            string secondItemId
        )
        {
            string key =
                CreateRecipeKey(
                    firstItemId,
                    secondItemId
                );

            if (!fusionRecipes.ContainsKey(key))
            {
                Debug.LogError(
                    $"[ShopFusion] 존재하지 않는 조합식 : " +
                    $"{firstItemId} + {secondItemId}"
                );

                return null;
            }

            string result =
                fusionRecipes[key];

            Debug.Log(
                $"[ShopFusion] 조합 성공 : " +
                $"{firstItemId} + {secondItemId} → {result}"
            );

            return result;
        }


        // A+B와 B+A를 동일한 조합으로 처리하기 위함
        private string CreateRecipeKey(
            string firstItemId,
            string secondItemId
        )
        {
            if (
                string.Compare(
                    firstItemId,
                    secondItemId
                ) < 0
            )
            {
                return firstItemId + "|" + secondItemId;
            }

            return secondItemId + "|" + firstItemId;
        }
    }
}