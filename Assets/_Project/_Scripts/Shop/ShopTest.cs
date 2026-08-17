using System.Collections.Generic;
using UnityEngine;

namespace ItemShopSystem
{
    public class ShopTest : MonoBehaviour
    {
        private void Start()
        {
            // 상점 4칸 생성
            List<string> items =
                ShopManager.Instance.OpenShop(4);

            Debug.Log("===== 상점 목록 =====");

            foreach (string item in items)
            {
                Debug.Log(item);
            }


            // 리롤
            List<string> rerollItems =
                ShopManager.Instance.Reroll(4);

            Debug.Log("===== 리롤 결과 =====");

            foreach (string item in rerollItems)
            {
                Debug.Log(item);
            }
        }
    }
}