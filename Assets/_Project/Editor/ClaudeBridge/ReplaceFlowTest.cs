using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Ujam.Runtime.Item;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;
using UJam.Runtime.UI;
using UnityEngine;

namespace UJam.EditorTools
{
    /// <summary>
    /// 개발용(플레이 모드 전용): 스킬칸 5개를 액티브로 채운 뒤 상점의 다른 액티브를 구매해
    /// "제거할 아이템을 고르십시오" 교체 흐름을 확인합니다.
    /// </summary>
    internal static class ReplaceFlowTest
    {
        private static int _shopSlot = -1;
        private static string _targetId;

        /// <summary>스킬칸을 채우고 상점 액티브 구매를 눌러 교체 선택 상태로 만듭니다.</summary>
        public static string Setup()
        {
            if (!Application.isPlaying) return "play mode only";
            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
            PlayerSkillManager skills = Object.FindAnyObjectByType<PlayerSkillManager>();
            UIShopBuy ui = Object.FindAnyObjectByType<UIShopBuy>();
            List<string> shop = CurrentShop();

            // 상점에 진열된 액티브 하나를 구매 대상으로 정한다.
            _shopSlot = shop.FindIndex(id => id != null && ItemCatalog.GetMeta(id)?.Kind == ItemKind.Active);
            if (_shopSlot < 0) return "shop has no active item; reroll and retry. shop=[" + string.Join(",", shop) + "]";
            _targetId = shop[_shopSlot];

            // 구매 대상이 아닌 액티브로 스킬칸 5개를 채운다.
            foreach (string id in ItemCatalog.IDs)
            {
                if (skills.EmptySlots == 0) break;
                if (id == _targetId || shop.Contains(id) || ItemCatalog.GetMeta(id).Kind != ItemKind.Active) continue;
                inventory.TryAdd(id);
            }

            long goldBefore = Wallet.Instance.Gold;
            ui.BuyItem(_shopSlot);
            UIItemReplacePrompt prompt = UIItemReplacePrompt.Instance;
            return $"target={_targetId}({ItemCatalog.GetMeta(_targetId).Name}) shopSlot={_shopSlot} emptySkillSlots={skills.EmptySlots} " +
                   $"skills=[{Skills(skills)}] promptSelecting={prompt != null && prompt.IsSelecting} goldBefore={goldBefore} goldNow={Wallet.Instance.Gold}";
        }

        /// <summary>교체 대상으로 스킬칸 하나를 고른 결과를 보고합니다.</summary>
        public static string Pick(int skillSlot)
        {
            PlayerInventory inventory = Object.FindAnyObjectByType<PlayerInventory>();
            PlayerSkillManager skills = Object.FindAnyObjectByType<PlayerSkillManager>();
            string before = skills.GetSkill(skillSlot)?.ID;
            long goldBefore = Wallet.Instance.Gold;
            bool picked = UIItemReplacePrompt.Instance != null && UIItemReplacePrompt.Instance.TryPick(skillSlot);
            List<string> shop = CurrentShop();
            return $"picked={picked} slot{skillSlot}: {before} -> {skills.GetSkill(skillSlot)?.ID} (target {_targetId}) " +
                   $"gold {goldBefore} -> {Wallet.Instance.Gold} items=[{string.Join(",", inventory.Items.Where(i => i != Item.NullId))}] " +
                   $"shopSlotSold={(_shopSlot >= 0 && shop[_shopSlot] == null)} removedBackInPool={before != null && AllIds().Contains(before)}";
        }

        private static string Skills(PlayerSkillManager skills) =>
            string.Join(",", Enumerable.Range(0, PlayerSkillManager.SlotCount).Select(i => skills.GetSkill(i)?.ID ?? "-"));

        private static List<string> CurrentShop() => Field<List<string>>("currentShopItems");
        private static List<string> AllIds() => Field<List<string>>("allItemIds");

        private static T Field<T>(string name)
        {
            const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic;
            object shopBuy = typeof(ShopManager).GetField("shopBuy", F).GetValue(ShopManager.Instance);
            return (T)shopBuy.GetType().GetField(name, F).GetValue(shopBuy);
        }
    }
}
