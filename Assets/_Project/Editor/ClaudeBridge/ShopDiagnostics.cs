using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Ujam.Runtime.Item;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;
using UnityEngine;

namespace UJam.EditorTools
{
    /// <summary>개발용 진단: 플레이 중 상점 구매가 실패하는 조건을 단계별로 보고합니다.</summary>
    internal static class ShopDiagnostics
    {
        public static string Report(int slot)
        {
            StringBuilder sb = new StringBuilder();
            ShopManager manager = ShopManager.Instance;
            if (manager == null) return "no ShopManager";
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            object shopBuy = typeof(ShopManager).GetField("shopBuy", F).GetValue(manager);
            PlayerInventory inventory = (PlayerInventory)typeof(ShopManager).GetField("playerInventory", F).GetValue(manager);
            var current = (List<string>)shopBuy.GetType().GetField("currentShopItems", F).GetValue(shopBuy);
            var all = (List<string>)shopBuy.GetType().GetField("allItemIds", F).GetValue(shopBuy);
            bool purchasing = (bool)shopBuy.GetType().GetField("isPurchasing", F).GetValue(shopBuy);
            sb.Append($"isPurchasing={purchasing} current=[{string.Join(",", current)}] allCount={all.Count}\n");
            sb.Append($"inventory={(inventory != null ? inventory.name + "#" + inventory.GetInstanceID() + " active=" + inventory.isActiveAndEnabled : "null")}");
            sb.Append($" sceneInventory#{Object.FindAnyObjectByType<PlayerInventory>()?.GetInstanceID()} wallet={(Wallet.Instance != null ? Wallet.Instance.Gold.ToString() : "null")}\n");
            if (slot < current.Count)
            {
                string id = current[slot];
                ItemMeta meta = id != null ? ItemCatalog.GetMeta(id) : null;
                sb.Append($"slot{slot} id={id} inAll={(id != null && all.Contains(id))} meta={(meta != null ? meta.Name + " price=" + meta.Price + " kind=" + meta.Kind : "null")}\n");
                if (inventory != null && id != null) sb.Append($"items=[{string.Join(",", inventory.Items)}] count={inventory.GetCount(id)}\n");
            }
            return sb.ToString();
        }
    }
}
