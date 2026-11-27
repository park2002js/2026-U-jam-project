using System;
using System.IO;
using System.Linq;
using TMPro;
using UJam.Runtime.Phase;
using UJam.Runtime.Player;
using UJam.Runtime.Shop;
using UJam.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UJam.EditorTools.LowPolyUI
{
    /// <summary>
    /// 문서(MainScene 임시 UI) 기준 HUD, 상점(Tablet), 결산 창, 타이틀+레벨 선택 화면을 로우폴리 UI 키트로 구성합니다.
    /// 모든 단계는 다시 실행해도 같은 결과가 나오도록(idempotent) 작성되어 있습니다.
    /// </summary>
    internal static class LowPolyUIBuilder
    {
        private const string UIDir = "Assets/_Project/Prefabs/UI";
        private const string MainScenePath = "Assets/_Project/Scenes/_MainScene.unity";
        private const string TitleScenePath = "Assets/_Project/Scenes/_TitleScene.unity";
        private const string LevelSelectScenePath = "Assets/_Project/Scenes/_LevelSelectScene.unity";
        private const string ResultPrefabPath = UIDir + "/GameResult_UI.prefab";

        private static Sprite White => LowPolyUIArt.Load("LP_White");

        [MenuItem("Tools/UJam/UI/Build All (Art + Prefabs + Scenes)", priority = 0)]
        public static void BuildAll()
        {
            LowPolyUIArt.GenerateAll();
            BuildHudPrefabs();
            BuildShopPrefabs();
            BuildResultPrefab();
            BuildSettingsAndGameOver();
            BuildTitleScene();
            ApplyMainScene();   // 프리팹 갱신으로 dirty가 된 메인 씬을 먼저 저장해야 테스트 씬을 열 수 있습니다.
            ApplyTestScenes();
            ApplyResolutionDefaults();
            Debug.Log("[LowPolyUIBuilder] 전체 UI 구성 완료");
        }

        // ================================================================== HUD

        [MenuItem("Tools/UJam/UI/2. Build HUD Prefabs", priority = 2)]
        public static void BuildHudPrefabs()
        {
            BuildItemComponent();
            BuildItemsUI();
            BuildSkillsUI();
            BuildStatPlate($"{UIDir}/Player_Gold_UI.prefab", "재화", "LP_Coin", new Vector2(69, 69), -80f, "1,250", UIKit.Gold, (root, amount) =>
            {
                UIPlayerGold gold = root.GetComponent<UIPlayerGold>();
                UIKit.SetRef(gold, "_currencyText", amount);
                UIKit.SetValue(gold, "_format", p => p.stringValue = "{0:N0}");
            });
            BuildStatPlate($"{UIDir}/Wave_RemainingEnemy_UI.prefab", "남은 적", "LP_Enemy", new Vector2(67, 69), -196f, "18", UIKit.TextMain, (root, amount) =>
            {
                UIKit.SetRef(root.GetComponent<UIRemainingEnemyCount>(), "_countText", amount);
            });
            BuildHpUI();
            BuildNextWaveButton();
            AssetDatabase.SaveAssets();
            Debug.Log("[LowPolyUIBuilder] HUD 프리팹 구성 완료");
        }

        /// <summary>모든 아이템 아이콘이 공유하는 정사각 프레임 컴포넌트입니다.</summary>
        private static void BuildItemComponent()
        {
            EditPrefab($"{UIDir}/Player_Item_Component.prefab", root =>
            {
                UIKit.ClearChildren(root.transform);
                RectTransform rt = (RectTransform)root.transform;
                rt.sizeDelta = new Vector2(99, 99);
                Image frame = UIKit.Image(root.transform, "Item_Frame", UIKit.Slot, Color.white, false, 1.3f);
                UIKit.Stretch(frame.rectTransform);
                Image icon = UIKit.Image(root.transform, "Item_Icon", null, Color.white);
                UIKit.Stretch(icon.rectTransform, 13, 13, 13, 13);
                icon.preserveAspect = true;
                icon.enabled = false;

                UIItemIcon component = UIKit.Ensure<UIItemIcon>(root);
                UIKit.SetRef(component, "_icon", icon);
                UIKit.SetRef(component, "_frame", frame);
                UIKit.SetRef(component, "_lockOverlay", null);
            });
        }

        /// <summary>
        /// 아이템 목록: 제목·보유 수·자물쇠·배경 창 없이 정사각 프레임과 아이콘만 4열로 배치합니다.
        /// 프레임 수는 인벤토리 보유 한도만큼만 보이고(기본 8칸 = 4x2), 한도 밖 칸은 숨깁니다.
        /// </summary>
        private static void BuildItemsUI()
        {
            GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{UIDir}/Player_Item_Component.prefab");
            EditPrefab($"{UIDir}/Player_Items_UI.prefab", root =>
            {
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);

                RectTransform grid = UIKit.Node("Grid", root.transform);
                UIKit.Place(grid, UIKit.TopLeft, UIKit.TopLeft, new Vector2(40, -80), new Vector2(414, 519));
                GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(99, 99);
                layout.spacing = new Vector2(6, 6);
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 4;
                layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
                layout.childAlignment = TextAnchor.UpperLeft;

                // 편집 화면 미리보기용 슬롯(실행 시 UIPlayerItems가 다시 생성). 보유 한도 12칸 = 4열 x 3줄.
                for (int i = 0; i < 12; i++)
                {
                    GameObject preview = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefab, grid);
                    preview.name = $"Item_Slot_{i + 1:00}";
                }

                UIPlayerItems component = root.GetComponent<UIPlayerItems>();
                UIKit.SetRef(component, "_root", grid);
                UIKit.SetRef(component, "_itemPrefab", itemPrefab.GetComponent<UIItemIcon>());
                UIKit.SetValue(component, "_frameCount", p => p.intValue = 20);
                UIKit.SetRef(component, "_capacityText", null);
                UIKit.SetValue(component, "_hideSlotsOverCapacity", p => p.boolValue = true);
            });
        }

        /// <summary>
        /// 스킬 창: 정사각 슬롯 5칸, 각각 숫자키 1~5에 대응합니다.
        /// 제목 없이 얇은 띠로 감싸 화면 맨 아래에 붙이고, 성벽과 약간의 틈이 생기도록 작게 만듭니다.
        /// </summary>
        private static void BuildSkillsUI()
        {
            const float pad = 8f, size = 88f, gap = 14f;
            const int count = 5;
            float rowWidth = size * count + gap * (count - 1);
            EditPrefab($"{UIDir}/Player_Skills_UI.prefab", root =>
            {
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);

                Image bar = UIKit.Image(root.transform, "Bar", UIKit.Panel, Color.white, false, 3.4f);
                UIKit.Place(bar.rectTransform, UIKit.BottomCenter, UIKit.BottomCenter, Vector2.zero, new Vector2(rowWidth + pad * 2f, size + pad * 2f));

                RectTransform row = UIKit.Node("Slots", bar.transform);
                UIKit.Place(row, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(rowWidth, size));
                Image[] highlights = new Image[count];
                for (int i = 0; i < count; i++)
                {
                    RectTransform slot = UIKit.Node($"SkillSlot_{i + 1}", row);
                    UIKit.Place(slot, UIKit.Left, UIKit.Left, new Vector2(i * (size + gap), 0), new Vector2(size, size));
                    // 교체 선택 중에만 켜지는 주황 테두리(프레임 뒤로 살짝 크게)
                    Image highlight = UIKit.Image(slot, "Pick_Highlight", UIKit.ButtonSprite, new Color(1f, 0.62f, 0.22f, 1f), false, 2.4f);
                    UIKit.Stretch(highlight.rectTransform, -7, -7, -7, -7);
                    highlight.gameObject.SetActive(false);
                    highlights[i] = highlight;
                    // 프레임은 클릭을 받아 교체 대상 선택(UISkillSlot.OnPointerClick)에 쓰입니다.
                    Image frame = UIKit.Image(slot, "Skill_Icon_Frame", UIKit.Slot, Color.white, true, 1.4f);
                    UIKit.Stretch(frame.rectTransform);
                    Image icon = UIKit.Image(slot, "Skill_Icon", null, Color.white);
                    UIKit.Stretch(icon.rectTransform, 12, 12, 12, 12);
                    icon.preserveAspect = true;
                    icon.enabled = false;
                    Image cooldown = UIKit.Image(slot, "Cooldown", White, new Color(0.04f, 0.03f, 0.07f, 0.72f));
                    UIKit.Stretch(cooldown.rectTransform, 12, 12, 12, 12);
                    cooldown.type = Image.Type.Filled;
                    cooldown.fillMethod = Image.FillMethod.Radial360;
                    cooldown.fillOrigin = (int)Image.Origin360.Top;
                    cooldown.fillClockwise = false;
                    cooldown.fillAmount = 0f;
                    cooldown.enabled = false;

                    Image key = UIKit.Image(slot, "Skill_Key", UIKit.ButtonSprite, UIKit.Slate, false, 2.8f);
                    UIKit.Place(key.rectTransform, UIKit.TopLeft, UIKit.Center, new Vector2(7, -7), new Vector2(36, 34));
                    TextMeshProUGUI keyText = UIKit.Text(key.transform, "Text", (i + 1).ToString(), 22, UIKit.TextMain);
                    UIKit.Stretch(keyText.rectTransform, 0, 0, 0, 3);

                    UISkillSlot skill = slot.gameObject.AddComponent<UISkillSlot>();
                    UIKit.SetValue(skill, "_slotIndex", p => p.intValue = i);
                    UIKit.SetRef(skill, "_skillIcon", icon);
                    UIKit.SetRef(skill, "_cooldownOverlay", cooldown);
                }

                // 스킬칸이 가득 찬 상태에서 액티브를 사면 스킬바 바로 위에 뜨는 교체 안내
                Image prompt = UIKit.Image(root.transform, "Replace_Prompt", UIKit.Panel, Color.white, false, 2.6f);
                UIKit.Place(prompt.rectTransform, UIKit.BottomCenter, UIKit.BottomCenter, new Vector2(0, size + pad * 2f + 8f), new Vector2(620, 72));
                Image newIcon = UIKit.Image(prompt.transform, "New_Item_Icon", null, Color.white);
                UIKit.Place(newIcon.rectTransform, UIKit.Left, UIKit.Center, new Vector2(46, 0), new Vector2(48, 48));
                newIcon.preserveAspect = true;
                TextMeshProUGUI title = UIKit.Text(prompt.transform, "Title", "제거할 아이템을 고르십시오", 28, UIKit.Gold, TextAlignmentOptions.Left);
                UIKit.Place(title.rectTransform, UIKit.Left, UIKit.Left, new Vector2(86, 10), new Vector2(510, 40));
                TextMeshProUGUI hint = UIKit.Text(prompt.transform, "Hint", "교체할 스킬칸을 클릭하세요  ·  우클릭 / ESC 취소", 18, UIKit.TextSub, TextAlignmentOptions.Left, false);
                UIKit.Place(hint.rectTransform, UIKit.Left, UIKit.Left, new Vector2(86, -17), new Vector2(510, 28));
                prompt.gameObject.SetActive(false);

                UIItemReplacePrompt replace = UIKit.Ensure<UIItemReplacePrompt>(root);
                UIKit.SetRef(replace, "_promptRoot", prompt.gameObject);
                UIKit.SetRef(replace, "_newItemIcon", newIcon);
                UIKit.SetValue(replace, "_slotHighlights", p =>
                {
                    p.arraySize = highlights.Length;
                    for (int i = 0; i < highlights.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = highlights[i];
                });
            });
        }

        /// <summary>문서: 재화/남은 적 표시창 459x98, 아이콘 약 69x69. "라벨 | 값" 구성.</summary>
        private static void BuildStatPlate(string path, string label, string iconName, Vector2 iconSize, float top, string sample, Color valueColor, Action<GameObject, TextMeshProUGUI> wire)
        {
            EditPrefab(path, root =>
            {
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);

                Image plate = UIKit.Image(root.transform, "Plate", UIKit.Panel, Color.white, false, 1.6f);
                UIKit.Place(plate.rectTransform, UIKit.TopRight, UIKit.TopRight, new Vector2(-32, top), new Vector2(459, 98));
                Image icon = UIKit.Image(plate.transform, "Icon", LowPolyUIArt.Load(iconName), Color.white);
                UIKit.Place(icon.rectTransform, UIKit.Left, UIKit.Center, new Vector2(62, 0), iconSize);
                TextMeshProUGUI labelText = UIKit.Text(plate.transform, "Label", label, 30, UIKit.TextMain, TextAlignmentOptions.Left);
                UIKit.Place(labelText.rectTransform, UIKit.Left, UIKit.Left, new Vector2(110, -1), new Vector2(130, 60));
                Image separator = UIKit.Image(plate.transform, "Separator", White, new Color(0.89f, 0.75f, 0.54f, 0.85f));
                UIKit.Place(separator.rectTransform, UIKit.Left, UIKit.Center, new Vector2(240, 0), new Vector2(4, 44));
                TextMeshProUGUI amount = UIKit.Text(plate.transform, "Amount", sample, 40, valueColor, TextAlignmentOptions.Right);
                UIKit.Place(amount.rectTransform, UIKit.Right, UIKit.Right, new Vector2(-30, -1), new Vector2(180, 70));
                wire(root, amount);
            });
        }

        /// <summary>문서: HP 전체 138x609, 게이지, 하트 아이콘 86x80. 'HP' 문구는 넣지 않습니다.</summary>
        private static void BuildHpUI()
        {
            EditPrefab($"{UIDir}/Player_HP_UI.prefab", root =>
            {
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);

                RectTransform group = UIKit.Node("HP_Group", root.transform);
                UIKit.Place(group, UIKit.TopRight, UIKit.TopRight, new Vector2(-54, -318), new Vector2(138, 609));
                Image frame = UIKit.Image(group, "HP_Bar_Background", LowPolyUIArt.Load("LP_HPFrame"), Color.white);
                UIKit.Place(frame.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -86), new Vector2(112, 523));
                Image fill = UIKit.Image(group, "HP_Bar", LowPolyUIArt.Load("LP_HPFill"), Color.white);
                UIKit.Place(fill.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -118), new Vector2(69, 420));
                fill.preserveAspect = false;
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Vertical;
                fill.fillOrigin = (int)Image.OriginVertical.Bottom;
                fill.fillAmount = 1f;
                Image medallion = UIKit.Image(group, "Heart_Medallion", LowPolyUIArt.Load("LP_Medallion"), Color.white);
                UIKit.Place(medallion.rectTransform, UIKit.TopCenter, UIKit.TopCenter, Vector2.zero, new Vector2(128, 128));
                Image heart = UIKit.Image(medallion.transform, "Heart", LowPolyUIArt.Load("LP_Heart"), Color.white);
                UIKit.Place(heart.rectTransform, UIKit.Center, UIKit.Center, new Vector2(0, -1), new Vector2(86, 80));

                UIPlayerHealth health = root.GetComponent<UIPlayerHealth>();
                UIKit.SetRef(health, "_hpBar", fill);
                UIKit.SetRef(health, "_pulseTarget", heart.rectTransform);
            });
        }

        /// <summary>정비 Phase의 '다음 웨이브 시작' 버튼: HP 게이지 아래 오른쪽 구석, Tablet과 함께 올라옵니다.</summary>
        private static void BuildNextWaveButton()
        {
            EditPrefab($"{UIDir}/NextWaveStart_Btn.prefab", root =>
            {
                Image image = root.GetComponent<Image>();
                Button button = root.GetComponent<Button>();
                UIKit.StyleButton(button, image, UIKit.ButtonOrange);
                UIKit.Place((RectTransform)root.transform, UIKit.BottomRight, UIKit.BottomRight, new Vector2(-36, 34), new Vector2(300, 96));
                TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);
                UIKit.ApplyText(text, "다음 웨이브 시작", 32, UIKit.TextDark, TextAlignmentOptions.Center, false);
                UIKit.Stretch(text.rectTransform, 16, 6, 16, 12);
                UIKit.Ensure<UITitleBtnFX>(root);
                UISlideIn slide = UIKit.Ensure<UISlideIn>(root);
                UIKit.SetValue(slide, "_delay", p => p.floatValue = 0.15f);
                UIKit.SetValue(slide, "_distance", p => p.floatValue = 320f);
                UIKit.SetValue(slide, "_overshoot", p => p.floatValue = 1.4f);
            });
        }

        // ================================================================== Shop (Tablet)

        [MenuItem("Tools/UJam/UI/3. Build Shop (Tablet)", priority = 3)]
        public static void BuildShopPrefabs()
        {
            BuildShopSlot();
            BuildShopBuyPage();
            BuildTabletMenu();
            BuildTabletFrame();
            AssetDatabase.SaveAssets();
            Debug.Log("[LowPolyUIBuilder] 상점(Tablet) 프리팹 구성 완료");
        }

        /// <summary>상품 카드: 공용 정사각 프레임 아이콘 + 가격 사각 박스(눌러야 결제되는 버튼).</summary>
        private static void BuildShopSlot()
        {
            EditPrefab($"{UIDir}/Tablet_ItemBuy_Slot.prefab", root =>
            {
                Image card = UIKit.Find(root.transform, "Item_Slot_Layout").GetComponent<Image>();
                UIKit.ApplyImage(card, UIKit.Panel, Color.white, false, 2f);

                RectTransform icon = (RectTransform)UIKit.Find(root.transform, "Item_Icon");
                Transform oldFrame = UIKit.Find(root.transform, "Item_Frame");
                if (oldFrame != null) Object.DestroyImmediate(oldFrame.gameObject);
                Image frame = UIKit.Image(card.transform, "Item_Frame", UIKit.Slot, Color.white, false, 1.1f);
                UIKit.Place(frame.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -16), new Vector2(168, 168));
                frame.transform.SetSiblingIndex(icon.GetSiblingIndex());
                UIKit.Place(icon, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -32), new Vector2(136, 136));
                icon.GetComponent<Image>().preserveAspect = true;
                icon.GetComponent<Image>().enabled = false; // 상품이 정해지면 UIShopItem.SetItem이 켭니다.

                RectTransform buy = (RectTransform)UIKit.Find(root.transform, "Item_Buy_Btn");
                UIKit.StyleButton(buy.GetComponent<Button>(), buy.GetComponent<Image>(), UIKit.ButtonGold);
                UIKit.Place(buy, UIKit.BottomCenter, UIKit.BottomCenter, new Vector2(0, 16), new Vector2(176, 64));
                Transform oldCoin = buy.Find("Coin_Icon");
                if (oldCoin != null) Object.DestroyImmediate(oldCoin.gameObject);
                Image coin = UIKit.Image(buy, "Coin_Icon", LowPolyUIArt.Load("LP_Coin"), Color.white);
                UIKit.Place(coin.rectTransform, UIKit.Left, UIKit.Center, new Vector2(34, 2), new Vector2(36, 36));
                TMP_Text price = UIKit.Find(root.transform, "Item_Price_Text").GetComponent<TMP_Text>();
                UIKit.ApplyText(price, "50", 30, new Color(0.17f, 0.11f, 0.05f, 1f), TextAlignmentOptions.Center, false);
                price.enableAutoSizing = true; // 'SOLD OUT'처럼 긴 문구도 박스 안에 맞춥니다.
                price.fontSizeMin = 16f;
                price.fontSizeMax = 30f;
                UIKit.Stretch(price.rectTransform, 52, 4, 14, 10);
                price.transform.SetAsLastSibling();
                UIKit.Ensure<UITitleBtnFX>(buy.gameObject);

                UIKit.SetRef(root.GetComponent<UIShopItem>(), "_coinIcon", coin.gameObject);
            });
        }

        /// <summary>구매 페이지: Tablet 테두리 안쪽에 맞춰 목록/리롤/설명 영역을 재배치하고 키트 스타일을 적용합니다.</summary>
        private static void BuildShopBuyPage()
        {
            EditPrefab($"{UIDir}/Tablet_ItemBuy_UI.prefab", root =>
            {
                // 상단 탭이 없어졌으므로 구매 영역을 Tablet 세로 가운데에 둡니다.
                RectTransform layout = (RectTransform)root.transform.Find("Layout");
                UIKit.ApplyImage(layout.GetComponent<Image>(), null, new Color(0, 0, 0, 0));
                layout.anchorMin = new Vector2(0, 0.5f);
                layout.anchorMax = new Vector2(1, 0.5f);
                layout.pivot = new Vector2(0.5f, 0.5f);
                layout.anchoredPosition = Vector2.zero;
                layout.sizeDelta = new Vector2(-64, 850);

                // 상품 목록: 안쪽 홈 배경 없이 카드만 두고, 카드 사이 간격을 넓혀 여유 있게 배치합니다.
                RectTransform list = (RectTransform)layout.Find("ItemList_Layout");
                UIKit.ApplyImage(list.GetComponent<Image>(), null, new Color(0, 0, 0, 0));
                UIKit.Place(list, UIKit.Center, UIKit.Center, new Vector2(0, 62), new Vector2(1090, 700));
                GridLayoutGroup grid = list.GetComponent<GridLayoutGroup>();
                grid.padding = new RectOffset(0, 0, 0, 0);
                grid.cellSize = new Vector2(200, 300);
                grid.spacing = new Vector2(130, 70);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.childAlignment = TextAnchor.MiddleCenter;

                RectTransform reroll = (RectTransform)layout.Find("ItemReRoll_Layout");
                UIKit.ApplyImage(reroll.GetComponent<Image>(), null, new Color(0, 0, 0, 0));
                UIKit.Place(reroll, UIKit.Center, UIKit.Center, new Vector2(0, -358), new Vector2(420, 84));
                RectTransform rerollButton = (RectTransform)reroll.Find("ItemReRoll_Btn");
                UIKit.StyleButton(rerollButton.GetComponent<Button>(), rerollButton.GetComponent<Image>(), UIKit.ButtonOrange);
                UIKit.Stretch(rerollButton, 0, 4, 0, 4);
                TMP_Text rerollText = rerollButton.GetComponentInChildren<TMP_Text>(true);
                UIKit.ApplyText(rerollText, "새로고침", 32, UIKit.TextDark, TextAlignmentOptions.Center, false);
                UIKit.Stretch(rerollText.rectTransform, 0, 4, 0, 10);

                // 오른쪽 안내 영역은 문구 없이 비우고, 영역 자체를 꺼서 상품 목록이 가로 공간을 넓게 쓰게 합니다.
                RectTransform description = (RectTransform)layout.Find("ItemDescription_Layout");
                foreach (string n in new[] { "Hint_Title", "Hint_Text", "Hint_Icon" })
                {
                    Transform old = description.Find(n);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
                description.gameObject.SetActive(false);

                RectTransform popup = (RectTransform)root.transform.Find("Item_Description_Popup");
                UIKit.ApplyImage(popup.GetComponent<Image>(), UIKit.Panel, Color.white, false, 2.6f);
                TMP_Text descriptionText = popup.GetComponentInChildren<TMP_Text>(true);
                UIKit.ApplyText(descriptionText, string.Empty, 22, UIKit.TextMain, TextAlignmentOptions.TopLeft, false);
                descriptionText.textWrappingMode = TextWrappingModes.Normal;
                UIKit.Stretch(descriptionText.rectTransform, 20, 16, 20, 16);
                popup.SetAsLastSibling();
            });
        }

        /// <summary>상단 메뉴 탭: 로우폴리 버튼, 선택 탭은 주황색.</summary>
        private static void BuildTabletMenu()
        {
            EditPrefab($"{UIDir}/Tablet_Menu_UI.prefab", root =>
            {
                UITabletMenu menu = root.GetComponent<UITabletMenu>();
                UIKit.SetValue(menu, "_selectedColor", p => p.colorValue = new Color(1f, 0.66f, 0.3f, 1f));

                RectTransform layout = (RectTransform)root.transform.Find("Layout");
                UIKit.ApplyImage(layout.GetComponent<Image>(), null, new Color(0, 0, 0, 0));
                layout.anchorMin = new Vector2(0, 1);
                layout.anchorMax = new Vector2(1, 1);
                layout.pivot = new Vector2(0.5f, 0.5f);
                layout.anchoredPosition = new Vector2(0, -72);
                layout.sizeDelta = new Vector2(-64, 84);
                HorizontalLayoutGroup h = layout.GetComponent<HorizontalLayoutGroup>();
                if (h != null)
                {
                    h.spacing = 12f;
                    h.childAlignment = TextAnchor.MiddleLeft;
                    h.childControlWidth = false;
                    h.childControlHeight = false;
                    h.childForceExpandWidth = false;
                    h.childForceExpandHeight = false;
                }

                foreach (Button button in layout.GetComponentsInChildren<Button>(true))
                {
                    Image image = button.GetComponent<Image>();
                    UIKit.StyleButton(button, image, Color.white);
                    ColorBlock colors = button.colors;
                    colors.normalColor = UIKit.Slate;
                    colors.selectedColor = UIKit.Slate;
                    colors.highlightedColor = Color.Lerp(UIKit.Slate, Color.white, 0.3f);
                    colors.pressedColor = Color.Lerp(UIKit.Slate, Color.black, 0.25f);
                    colors.disabledColor = new Color(0.32f, 0.3f, 0.38f, 0.75f);
                    button.colors = colors;
                    ((RectTransform)button.transform).sizeDelta = new Vector2(166, 72);

                    // 기능이 없는 자리표시 탭(None_Btn)은 숨겨 상단을 비웁니다.
                    bool placeholder = button.name.StartsWith("None");
                    if (placeholder) button.interactable = false;
                    button.gameObject.SetActive(!placeholder);
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                    {
                        string text = placeholder ? "준비 중" : label.text.Trim();
                        UIKit.ApplyText(label, text, 26, placeholder ? UIKit.TextSub : UIKit.TextMain, TextAlignmentOptions.Center, !placeholder);
                        UIKit.Stretch(label.rectTransform, 6, 2, 6, 8);
                    }
                }
            });
        }

        /// <summary>
        /// Tablet 본체: 로우폴리 패널을 화면 가운데에 두고, 상단 탭(구매/강화/조합)은 숨깁니다.
        /// 정비 Phase 진입 시 아래에서 위로 올라오는 연출(UISlideIn)을 붙입니다.
        /// </summary>
        private static void BuildTabletFrame()
        {
            EditPrefab($"{UIDir}/Tablet_Frame_UI.prefab", root =>
            {
                RectTransform layout = (RectTransform)root.transform.Find("Layout");
                UIKit.ApplyImage(layout.GetComponent<Image>(), UIKit.Panel, Color.white, true);
                layout.localScale = Vector3.one * 0.76f;
                layout.anchoredPosition = Vector2.zero;
                layout.sizeDelta = new Vector2(1150, 930);

                // 상단 탭 메뉴는 사용하지 않으므로 끕니다(UITablet은 꺼진 메뉴에도 안전하게 동작).
                Transform menu = layout.Find("Tablet_Menu_UI");
                if (menu != null) menu.gameObject.SetActive(false);

                UISlideIn slide = UIKit.Ensure<UISlideIn>(root);
                UIKit.SetRef(slide, "_target", layout);
                UIKit.SetValue(slide, "_duration", p => p.floatValue = 0.55f);
                UIKit.SetValue(slide, "_overshoot", p => p.floatValue = 0.9f);
                UIKit.SetValue(slide, "_distance", p => p.floatValue = 0f);
            });
        }

        // ================================================================== Result

        [MenuItem("Tools/UJam/UI/4. Build Result Prefab", priority = 4)]
        public static void BuildResultPrefab()
        {
            bool exists = File.Exists(ResultPrefabPath);
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(ResultPrefabPath) : new GameObject("GameResult_UI", typeof(RectTransform));
            try
            {
                root.layer = 5;
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);
                UIGameResult result = UIKit.Ensure<UIGameResult>(root);

                RectTransform content = UIKit.Node("Content", root.transform);
                UIKit.Stretch(content);
                Image dim = UIKit.Image(content, "Dim", White, UIKit.Dim, true);
                UIKit.Stretch(dim.rectTransform);

                Image window = UIKit.Image(content, "Window", UIKit.Panel, Color.white, true);
                UIKit.Place(window.rectTransform, UIKit.Center, UIKit.Center, new Vector2(0, -16), new Vector2(760, 650));

                Image banner = UIKit.Image(window.transform, "Banner", LowPolyUIArt.Load("LP_Banner"), Color.white);
                UIKit.Place(banner.rectTransform, UIKit.TopCenter, UIKit.Center, new Vector2(0, 8), new Vector2(640, 150));
                TextMeshProUGUI title = UIKit.Text(banner.transform, "Title", "STAGE CLEAR", 56, Color.white);
                title.characterSpacing = 4f;
                UIKit.Stretch(title.rectTransform, 90, 16, 90, 38);

                TextMeshProUGUI subtitle = UIKit.Text(window.transform, "Subtitle", "모든 웨이브 클리어  ·  5 / 5", 26, UIKit.TextSub, TextAlignmentOptions.Center, false);
                UIKit.Place(subtitle.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -100), new Vector2(640, 40));
                Image divider = UIKit.Image(window.transform, "Divider", LowPolyUIArt.Load("LP_Divider"), Color.white);
                UIKit.Place(divider.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -146), new Vector2(620, 24));

                (string label, string icon, string sample, Color color)[] stats =
                {
                    ("쓰러뜨린 적", "LP_Enemy", "128", UIKit.TextMain),
                    ("클리어 시간", "LP_Clock", "12:34", UIKit.TextMain),
                    ("사용한 금액", "LP_Coin", "2,450", UIKit.Gold),
                };
                RectTransform[] rows = new RectTransform[stats.Length];
                TextMeshProUGUI[] values = new TextMeshProUGUI[stats.Length];
                for (int i = 0; i < stats.Length; i++)
                {
                    Image row = UIKit.Image(window.transform, $"Stat_{i + 1}", UIKit.Slot, Color.white, false, 1.4f);
                    UIKit.Place(row.rectTransform, UIKit.TopCenter, UIKit.Center, new Vector2(0, -232 - i * 104), new Vector2(620, 92));
                    Image icon = UIKit.Image(row.transform, "Icon", LowPolyUIArt.Load(stats[i].icon), Color.white);
                    UIKit.Place(icon.rectTransform, UIKit.Left, UIKit.Center, new Vector2(56, 0), new Vector2(60, 60));
                    TextMeshProUGUI label = UIKit.Text(row.transform, "Label", stats[i].label, 30, UIKit.TextMain, TextAlignmentOptions.Left);
                    UIKit.Place(label.rectTransform, UIKit.Left, UIKit.Left, new Vector2(104, -1), new Vector2(260, 56));
                    TextMeshProUGUI value = UIKit.Text(row.transform, "Value", stats[i].sample, 42, stats[i].color, TextAlignmentOptions.Right);
                    UIKit.Place(value.rectTransform, UIKit.Right, UIKit.Right, new Vector2(-34, -1), new Vector2(240, 64));
                    rows[i] = row.rectTransform;
                    values[i] = value;
                }

                (string label, Color tint, Color text)[] buttons =
                {
                    ("레벨 선택", UIKit.Slate, UIKit.TextMain),
                    ("다시 하기", UIKit.ButtonGold, UIKit.TextDark),
                    ("타이틀로", UIKit.Slate, UIKit.TextMain),
                };
                Button[] made = new Button[buttons.Length];
                for (int i = 0; i < buttons.Length; i++)
                {
                    made[i] = UIKit.Button(window.transform, $"Btn_{i + 1}", buttons[i].tint, buttons[i].label, 30, buttons[i].text, out _, buttons[i].text == UIKit.TextMain);
                    UIKit.Place((RectTransform)made[i].transform, UIKit.BottomCenter, UIKit.Center, new Vector2((i - 1) * 232f, 74), new Vector2(212, 78));
                }

                content.gameObject.SetActive(false);

                UIKit.SetRef(result, "_root", content.gameObject);
                UIKit.SetRef(result, "_window", window.rectTransform);
                UIKit.SetRef(result, "_dim", dim);
                UIKit.SetRef(result, "_subtitleText", subtitle);
                UIKit.SetRef(result, "_killsText", values[0]);
                UIKit.SetRef(result, "_timeText", values[1]);
                UIKit.SetRef(result, "_spentText", values[2]);
                UIKit.SetValue(result, "_statRows", p =>
                {
                    p.arraySize = rows.Length;
                    for (int i = 0; i < rows.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
                });
                UIKit.SetRef(result, "_levelSelectButton", made[0]);
                UIKit.SetRef(result, "_retryButton", made[1]);
                UIKit.SetRef(result, "_titleButton", made[2]);

                PrefabUtility.SaveAsPrefabAsset(root, ResultPrefabPath);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LowPolyUIBuilder] 결산 창 프리팹 구성 완료: " + ResultPrefabPath);
        }

        // ================================================================== Title + Level select

        [MenuItem("Tools/UJam/UI/5. Build Title + Level Select", priority = 5)]
        public static void BuildTitleScene()
        {
            Sprite levelThumb = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Art/LevelSelect/Level1_BG_v1.0.png").OfType<Sprite>().FirstOrDefault();
            EditPrefab($"{UIDir}/Title_UI.prefab", root =>
            {
                UITitleManager manager = root.GetComponent<UITitleManager>();
                Transform buttons = root.transform.Find("Btn_Layout");

                (string name, string label)[] titleButtons = { ("GameStart", "게임 시작"), ("Option", "설정"), ("Exit", "게임 종료") };
                for (int i = 0; i < titleButtons.Length; i++)
                {
                    Transform t = buttons.Find(titleButtons[i].name);
                    if (t == null) continue;
                    // 타이틀 버튼은 기존 결정대로 테두리 없는 글자형을 유지합니다. 투명 Image는 클릭 영역만 넓힙니다.
                    bool primary = i == 0;
                    Button button = t.GetComponent<Button>();
                    Image hitArea = t.GetComponent<Image>();
                    UIKit.StyleButton(button, hitArea, Color.white);
                    hitArea.enabled = true;
                    UIKit.ApplyImage(hitArea, null, new Color(1f, 1f, 1f, 0f), true);
                    UIKit.Place((RectTransform)t, UIKit.Center, UIKit.Center, new Vector2(0, -150 - i * 96), new Vector2(360, 80));
                    t.localScale = Vector3.one;
                    TMP_Text label = t.GetComponentInChildren<TMP_Text>(true);
                    UIKit.ApplyText(label, titleButtons[i].label, primary ? 46 : 38, primary ? UIKit.Gold : UIKit.TextMain, TextAlignmentOptions.Center, true);
                    UIKit.Stretch(label.rectTransform, 0, 0, 0, 6);
                    button.targetGraphic = label;
                }

                Transform oldPanel = root.transform.Find("LevelSelect_Panel");
                if (oldPanel != null) Object.DestroyImmediate(oldPanel.gameObject);

                RectTransform panel = UIKit.Node("LevelSelect_Panel", root.transform);
                UIKit.Place(panel, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(1920, 1080));
                Image dim = UIKit.Image(panel, "Dim", White, new Color(0.03f, 0.02f, 0.06f, 0.55f), true);
                UIKit.Stretch(dim.rectTransform, -400, -400, -400, -400);

                Image window = UIKit.Image(panel, "Window", UIKit.Panel, Color.white, true);
                UIKit.Place(window.rectTransform, UIKit.Center, UIKit.Center, new Vector2(0, -70), new Vector2(1180, 720));
                UIKit.HeaderPlate(window.transform, "레벨 선택", null, 420, -20, 36);

                Button back = UIKit.Button(window.transform, "Back_Btn", UIKit.Slate, string.Empty, 20, UIKit.TextMain, out _);
                UIKit.Place((RectTransform)back.transform, UIKit.TopLeft, UIKit.TopLeft, new Vector2(30, -22), new Vector2(84, 72));
                Image backIcon = UIKit.Image(back.transform, "Icon", LowPolyUIArt.Load("LP_Back"), UIKit.TextMain);
                UIKit.Place(backIcon.rectTransform, UIKit.Center, UIKit.Center, new Vector2(0, 2), new Vector2(40, 40));
                UIKit.AddClick(back, manager.OnClickBack);

                UILevelSelectManager levelSelect = UIKit.Ensure<UILevelSelectManager>(panel.gameObject);
                UIKit.SetValue(levelSelect, "level1SceneName", p => p.stringValue = "_MainScene");
                UIKit.SetValue(levelSelect, "level2SceneName", p => p.stringValue = string.Empty);
                UIKit.SetValue(levelSelect, "level3SceneName", p => p.stringValue = string.Empty);

                RectTransform cards = UIKit.Node("Cards", window.transform);
                UIKit.Place(cards, UIKit.Center, UIKit.Center, new Vector2(0, -40), new Vector2(1100, 560));
                for (int i = 0; i < 3; i++)
                {
                    Image card = UIKit.Image(cards, $"Level_{i + 1}_Card", UIKit.Panel, Color.white, false, 2f);
                    UIKit.Place(card.rectTransform, UIKit.Left, UIKit.Left, new Vector2(i * 380f, 0), new Vector2(340, 560));

                    TextMeshProUGUI levelLabel = UIKit.Text(card.transform, "Level_Label", $"LEVEL {i + 1}", 24, UIKit.Orange);
                    levelLabel.characterSpacing = 6f;
                    UIKit.Place(levelLabel.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -20), new Vector2(300, 36));

                    Image thumbFrame = UIKit.Image(card.transform, "Thumb_Frame", UIKit.Slot, Color.white);
                    UIKit.Place(thumbFrame.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -62), new Vector2(276, 276));
                    RectTransform mask = UIKit.Node("Thumb_Mask", thumbFrame.transform);
                    UIKit.Stretch(mask, 15, 15, 15, 15);
                    mask.gameObject.AddComponent<RectMask2D>();
                    Image thumb = UIKit.Image(mask, "Thumb", levelThumb, Color.white);
                    thumb.preserveAspect = false;
                    UIKit.Place(thumb.rectTransform, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(342, 248));
                    Image lockImage = UIKit.Image(thumbFrame.transform, "Lock", LowPolyUIArt.Load("LP_Lock"), Color.white);
                    UIKit.Place(lockImage.rectTransform, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(104, 104));
                    lockImage.gameObject.SetActive(i > 0);

                    TextMeshProUGUI name = UIKit.Text(card.transform, "Name", $"레벨 {i + 1}", 34, UIKit.TextMain);
                    UIKit.Place(name.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -352), new Vector2(300, 50));

                    Button play = UIKit.Button(card.transform, "Play_Btn", UIKit.ButtonGold, i == 0 ? "출격" : "준비 중", 32, UIKit.TextDark, out TextMeshProUGUI playLabel);
                    UIKit.Place((RectTransform)play.transform, UIKit.BottomCenter, UIKit.BottomCenter, new Vector2(0, 30), new Vector2(240, 78));
                    if (i > 0)
                    {
                        play.interactable = false;
                        thumb.color = new Color(0.35f, 0.33f, 0.42f, 1f);
                    }

                    UILevelCard levelCard = card.gameObject.AddComponent<UILevelCard>();
                    UIKit.SetRef(levelCard, "_manager", levelSelect);
                    int level = i + 1;
                    UIKit.SetValue(levelCard, "_level", p => p.intValue = level);
                    UIKit.SetRef(levelCard, "_playButton", play);
                    UIKit.SetRef(levelCard, "_playLabel", playLabel);
                    UIKit.SetRef(levelCard, "_lockOverlay", lockImage.gameObject);
                    UIKit.SetRef(levelCard, "_thumbnail", thumb);
                }

                UISlideIn slide = UIKit.Ensure<UISlideIn>(panel.gameObject);
                UIKit.SetRef(slide, "_target", window.rectTransform);
                UIKit.SetValue(slide, "_duration", p => p.floatValue = 0.5f);
                panel.gameObject.SetActive(false);

                UIKit.SetRef(manager, "_titleMenu", buttons.gameObject);
                UIKit.SetRef(manager, "_levelSelectPanel", panel.gameObject);

                // 설정 버튼으로 여는 설정창(타이틀에서는 게임을 멈출 필요가 없음)
                Transform oldSettings = root.transform.Find("Settings_UI");
                if (oldSettings != null) Object.DestroyImmediate(oldSettings.gameObject);
                GameObject settingsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
                if (settingsPrefab != null)
                {
                    GameObject settings = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, root.transform);
                    settings.name = "Settings_UI";
                    UIKit.Place((RectTransform)settings.transform, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(1920, 1080));
                    settings.transform.SetAsLastSibling();
                    UISettingsPanel settingsPanel = settings.GetComponent<UISettingsPanel>();
                    UIKit.SetValue(settingsPanel, "_pauseGame", p => p.boolValue = false);
                    UIKit.SetRef(manager, "_settingsPanel", settingsPanel);
                }
            });

            // 레벨 선택 씬은 타이틀 씬에 합쳐졌으므로 빌드 목록에서 제외합니다(파일은 보존).
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Any(s => s.path == LevelSelectScenePath))
            {
                EditorBuildSettings.scenes = scenes.Where(s => s.path != LevelSelectScenePath).ToArray();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[LowPolyUIBuilder] 타이틀 + 레벨 선택 통합 완료");
        }

        // ================================================================== Resolution

        /// <summary>
        /// 1920x1080 기준을 프로젝트 전체에 맞춥니다.
        /// - 타이틀 씬 캔버스: 메인 씬과 같은 '화면 크기에 맞춰 늘림, 기준 1920x1080, 가로 맞춤'
        /// - 빌드 기본 해상도: 1920x1080 (모니터 원래 해상도 대신 1920x1080으로 실행, 전체 화면 창 모드 유지)
        /// </summary>
        [MenuItem("Tools/UJam/UI/9. Apply 1920x1080 Defaults", priority = 9)]
        public static void ApplyResolutionDefaults()
        {
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            AssetDatabase.SaveAssets();

            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && active.path != TitleScenePath)
            {
                Debug.LogError($"[LowPolyUIBuilder] 현재 씬({active.path})에 저장되지 않은 변경이 있습니다. 저장 후 다시 실행하세요.");
                return;
            }
            string returnTo = active.path;
            Scene title = active.path == TitleScenePath ? active : EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            foreach (CanvasScaler scaler in title.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CanvasScaler>(true)))
            {
                if (!scaler.GetComponent<Canvas>().isRootCanvas) continue;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
                EditorUtility.SetDirty(scaler);
            }
            EditorSceneManager.MarkSceneDirty(title);
            EditorSceneManager.SaveScene(title);
            if (!string.IsNullOrEmpty(returnTo) && returnTo != TitleScenePath) EditorSceneManager.OpenScene(returnTo, OpenSceneMode.Single);
            Debug.Log("[LowPolyUIBuilder] 1920x1080 기준 적용 완료(타이틀 캔버스 · 빌드 기본 해상도)");
        }

        // ================================================================== Settings + Game Over

        private const string SettingsPrefabPath = UIDir + "/Settings_UI.prefab";

        [MenuItem("Tools/UJam/UI/8. Build Settings + Game Over", priority = 8)]
        public static void BuildSettingsAndGameOver()
        {
            BuildSettingsPrefab();
            BuildGameOverPrefab();
            AssetDatabase.SaveAssets();
            Debug.Log("[LowPolyUIBuilder] 설정창·게임 오버 창 구성 완료");
        }

        /// <summary>설정창: 배경음·효과음 볼륨 슬라이더 + 닫기 버튼.</summary>
        private static void BuildSettingsPrefab()
        {
            bool exists = File.Exists(SettingsPrefabPath);
            GameObject root = exists ? PrefabUtility.LoadPrefabContents(SettingsPrefabPath) : new GameObject("Settings_UI", typeof(RectTransform));
            try
            {
                root.layer = 5;
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);
                UISettingsPanel panelComponent = UIKit.Ensure<UISettingsPanel>(root);

                RectTransform panel = UIKit.Node("Panel", root.transform);
                UIKit.Stretch(panel);
                Image dim = UIKit.Image(panel, "Dim", White, UIKit.Dim, true);
                UIKit.Stretch(dim.rectTransform, -400, -400, -400, -400);

                Image window = UIKit.Image(panel, "Window", UIKit.Panel, Color.white, true);
                UIKit.Place(window.rectTransform, UIKit.Center, UIKit.Center, Vector2.zero, new Vector2(660, 430));
                UIKit.HeaderPlate(window.transform, "설정", null, 360, -20, 36);

                (string label, string name)[] rows = { ("배경음", "Bgm"), ("효과음", "Sfx") };
                Slider[] sliders = new Slider[rows.Length];
                TextMeshProUGUI[] values = new TextMeshProUGUI[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    float y = -140 - i * 92;
                    TextMeshProUGUI label = UIKit.Text(window.transform, $"{rows[i].name}_Label", rows[i].label, 30, UIKit.TextMain, TextAlignmentOptions.Left);
                    UIKit.Place(label.rectTransform, UIKit.TopLeft, new Vector2(0, 0.5f), new Vector2(54, y), new Vector2(120, 50));
                    Slider slider = UIKit.Slider(window.transform, $"{rows[i].name}_Slider", new Vector2(330, 44));
                    UIKit.Place((RectTransform)slider.transform, UIKit.TopLeft, new Vector2(0, 0.5f), new Vector2(180, y), new Vector2(330, 44));
                    TextMeshProUGUI value = UIKit.Text(window.transform, $"{rows[i].name}_Value", "80%", 28, UIKit.Gold, TextAlignmentOptions.Right);
                    UIKit.Place(value.rectTransform, UIKit.TopRight, new Vector2(1, 0.5f), new Vector2(-50, y), new Vector2(100, 50));
                    sliders[i] = slider;
                    values[i] = value;
                }

                Button close = UIKit.Button(window.transform, "Close_Btn", UIKit.ButtonGold, "닫기", 30, UIKit.TextDark, out _);
                UIKit.Place((RectTransform)close.transform, UIKit.BottomCenter, UIKit.Center, new Vector2(0, 70), new Vector2(220, 76));

                panel.gameObject.SetActive(false);

                UIKit.SetRef(panelComponent, "_panel", panel.gameObject);
                UIKit.SetRef(panelComponent, "_bgmSlider", sliders[0]);
                UIKit.SetRef(panelComponent, "_sfxSlider", sliders[1]);
                UIKit.SetRef(panelComponent, "_bgmValueText", values[0]);
                UIKit.SetRef(panelComponent, "_sfxValueText", values[1]);
                UIKit.SetRef(panelComponent, "_closeButton", close);
                UIKit.SetValue(panelComponent, "_pauseGame", p => p.boolValue = true);

                PrefabUtility.SaveAsPrefabAsset(root, SettingsPrefabPath);
            }
            finally
            {
                if (exists) PrefabUtility.UnloadPrefabContents(root);
                else Object.DestroyImmediate(root);
            }
        }

        /// <summary>게임 오버 창: 결산 창과 같은 로우폴리 구성 + 다시 하기 / 타이틀로 버튼.</summary>
        private static void BuildGameOverPrefab()
        {
            EditPrefab($"{UIDir}/GameOver_UI.prefab", root =>
            {
                UIKit.ClearChildren(root.transform);
                UIKit.Stretch((RectTransform)root.transform);
                UIGameOver gameOver = root.GetComponent<UIGameOver>();

                RectTransform content = UIKit.Node("Content", root.transform);
                UIKit.Stretch(content);
                Image dim = UIKit.Image(content, "Dim", White, new Color(0.08f, 0.02f, 0.04f, 0.75f), true);
                UIKit.Stretch(dim.rectTransform, -400, -400, -400, -400);

                Image window = UIKit.Image(content, "Window", UIKit.Panel, Color.white, true);
                UIKit.Place(window.rectTransform, UIKit.Center, UIKit.Center, new Vector2(0, -16), new Vector2(700, 320));

                Image banner = UIKit.Image(window.transform, "Banner", LowPolyUIArt.Load("LP_Banner"), new Color(0.86f, 0.42f, 0.4f, 1f));
                UIKit.Place(banner.rectTransform, UIKit.TopCenter, UIKit.Center, new Vector2(0, 8), new Vector2(600, 150));
                TextMeshProUGUI title = UIKit.Text(banner.transform, "Title", "GAME OVER", 56, Color.white);
                title.characterSpacing = 4f;
                UIKit.Stretch(title.rectTransform, 90, 16, 90, 38);

                TextMeshProUGUI subtitle = UIKit.Text(window.transform, "Subtitle", "웨이브 2 / 4 에서 기지가 무너졌습니다", 26, UIKit.TextSub, TextAlignmentOptions.Center, false);
                UIKit.Place(subtitle.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -100), new Vector2(640, 40));
                Image divider = UIKit.Image(window.transform, "Divider", LowPolyUIArt.Load("LP_Divider"), Color.white);
                UIKit.Place(divider.rectTransform, UIKit.TopCenter, UIKit.TopCenter, new Vector2(0, -146), new Vector2(560, 24));
                Button retry = UIKit.Button(window.transform, "Retry_Btn", UIKit.ButtonGold, "다시 하기", 30, UIKit.TextDark, out _);
                UIKit.Place((RectTransform)retry.transform, UIKit.BottomCenter, UIKit.Center, new Vector2(-130, 74), new Vector2(230, 78));
                Button title2 = UIKit.Button(window.transform, "Title_Btn", UIKit.Slate, "타이틀로", 30, UIKit.TextMain, out _, true);
                UIKit.Place((RectTransform)title2.transform, UIKit.BottomCenter, UIKit.Center, new Vector2(130, 74), new Vector2(230, 78));

                content.gameObject.SetActive(false);

                UIKit.SetRef(gameOver, "_gameOverUI", content.gameObject);
                UIKit.SetRef(gameOver, "_window", window.rectTransform);
                UIKit.SetRef(gameOver, "_subtitleText", subtitle);
                UIKit.SetRef(gameOver, "_retryButton", retry);
                UIKit.SetRef(gameOver, "_titleButton", title2);
            });
        }

        /// <summary>설정창 프리팹 인스턴스를 부모 아래에 하나만 두고 반환합니다.</summary>
        private static UISettingsPanel EnsureSettingsInstance(Transform parent)
        {
            UISettingsPanel existing = parent.GetComponentsInChildren<UISettingsPanel>(true).FirstOrDefault();
            if (existing != null) return existing;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            return instance.GetComponent<UISettingsPanel>();
        }

        // ================================================================== Main scene wiring

        private static readonly string[] TestScenePaths =
        {
            "Assets/_Project/Scenes/LCH_Scene.unity",
            "Assets/_Project/Scenes/PJS_Scene.unity",
        };

        [MenuItem("Tools/UJam/UI/6. Apply Main Scene Layout", priority = 6)]
        public static void ApplyMainScene() => ApplySceneLayout(MainScenePath);

        /// <summary>HUD 프리팹을 함께 쓰는 개인 테스트 씬(LCH, PJS)에도 메인 씬과 같은 배치·연결을 적용합니다.</summary>
        [MenuItem("Tools/UJam/UI/7. Apply Test Scenes (LCH, PJS)", priority = 7)]
        public static void ApplyTestScenes()
        {
            string returnTo = SceneManager.GetActiveScene().path;
            foreach (string path in TestScenePaths)
            {
                if (File.Exists(path)) ApplySceneLayout(path);
            }
            if (!string.IsNullOrEmpty(returnTo) && SceneManager.GetActiveScene().path != returnTo && !SceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.OpenScene(returnTo, OpenSceneMode.Single);
            }
        }

        /// <summary>
        /// 씬의 HUD 프리팹 인스턴스를 화면 전체에 맞추고, 씬 오브젝트 참조와 결산 창/UIManager 연결을 설정한 뒤 저장합니다.
        /// </summary>
        public static void ApplySceneLayout(string scenePath)
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && active.path != scenePath)
            {
                Debug.LogError($"[LowPolyUIBuilder] 현재 씬({active.path})에 저장되지 않은 변경이 있습니다. 저장 후 다시 실행하세요.");
                return;
            }

            Scene scene = active.path == scenePath ? active : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Transform[] all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            GameObject Find(string name) => all.FirstOrDefault(t => t != null && t.name == name)?.gameObject;
            T FindComp<T>() where T : Component => all.Where(t => t != null).Select(t => t.GetComponent<T>()).FirstOrDefault(c => c != null);

            PlayerInventory inventory = FindComp<PlayerInventory>();
            Wallet wallet = FindComp<Wallet>();
            PlayerStatus status = FindComp<PlayerStatus>();
            PhaseSystem phase = FindComp<PhaseSystem>();
            PlayerSkillManager skills = FindComp<PlayerSkillManager>();

            foreach (string hud in new[] { "Player_Items_UI", "Player_Skills_UI", "Player_Gold_UI", "Wave_RemainingEnemy_UI", "Player_HP_UI" })
            {
                GameObject go = Find(hud);
                if (go == null)
                {
                    Debug.LogWarning($"[LowPolyUIBuilder] {scenePath}에서 {hud}를 찾지 못했습니다.");
                    continue;
                }
                UIKit.Stretch((RectTransform)go.transform);
            }

            foreach (UIPlayerItems c in all.Where(t => t != null).Select(t => t.GetComponent<UIPlayerItems>()).Where(c => c != null))
            {
                RevertFields(c, "_root", "_itemPrefab", "_frameCount", "_capacityText", "_hideSlotsOverCapacity");
                UIKit.SetRef(c, "_inventory", inventory);
            }
            foreach (UIPlayerGold c in all.Where(t => t != null).Select(t => t.GetComponent<UIPlayerGold>()).Where(c => c != null))
            {
                RevertFields(c, "_currencyText", "_format");
                UIKit.SetRef(c, "_wallet", wallet);
            }
            foreach (UIPlayerHealth c in all.Where(t => t != null).Select(t => t.GetComponent<UIPlayerHealth>()).Where(c => c != null))
            {
                RevertFields(c, "_hpBar", "_pulseTarget");
                UIKit.SetRef(c, "_playerStatus", status);
            }
            foreach (UIRemainingEnemyCount c in all.Where(t => t != null).Select(t => t.GetComponent<UIRemainingEnemyCount>()).Where(c => c != null))
            {
                RevertFields(c, "_countText");
                UIKit.SetRef(c, "_phaseSystem", phase);
            }
            foreach (UISkillSlot c in all.Where(t => t != null).Select(t => t.GetComponent<UISkillSlot>()).Where(c => c != null))
            {
                UIKit.SetRef(c, "_skillManager", skills);
            }
            foreach (UIItemReplacePrompt c in all.Where(t => t != null).Select(t => t.GetComponent<UIItemReplacePrompt>()).Where(c => c != null))
            {
                UIKit.SetRef(c, "_skillManager", skills);
            }

            // 아이템 보유 한도: 12칸(왼쪽 목록 4열 x 3줄)
            if (inventory != null) UIKit.SetValue(inventory, "_slotCount", p => p.intValue = 12);

            // 프리팹 루트의 RectTransform 값은 씬에 항상 저장되므로(기본 오버라이드) 직접 지정합니다.
            GameObject nextWave = Find("NextWaveStart_Btn");
            if (nextWave != null)
            {
                UIKit.Place((RectTransform)nextWave.transform, UIKit.BottomRight, UIKit.BottomRight, new Vector2(-36, 34), new Vector2(300, 96));
            }

            // 기존 일시정지(설정) 버튼: 로우폴리 버튼으로 바꾸고 재화 창 위 오른쪽 구석으로 옮깁니다.
            Transform settingButton = Find("Setting_Btn")?.transform.Find("Button");
            if (settingButton != null)
            {
                UIKit.StyleButton(settingButton.GetComponent<Button>(), settingButton.GetComponent<Image>(), UIKit.Slate);
                UIKit.Place((RectTransform)settingButton, UIKit.TopRight, UIKit.TopRight, new Vector2(-32, -10), new Vector2(64, 62));
                Transform oldIcon = settingButton.Find("Icon");
                if (oldIcon != null) Object.DestroyImmediate(oldIcon.gameObject);
                Image pauseIcon = UIKit.Image(settingButton, "Icon", LowPolyUIArt.Load("LP_Pause"), UIKit.TextMain);
                UIKit.Place(pauseIcon.rectTransform, UIKit.Center, UIKit.Center, new Vector2(0, 2), new Vector2(30, 30));
                UIKit.Ensure<UITitleBtnFX>(settingButton.gameObject);
            }

            // 설정창(배경음·효과음): 캔버스 맨 위에 두고 설정(일시정지) 버튼으로 엽니다. 열려 있는 동안 게임을 멈춥니다.
            Transform settingsParent = Find("Preparation_UIs")?.transform.parent;
            if (settingsParent != null && File.Exists(SettingsPrefabPath))
            {
                UISettingsPanel settingsPanel = EnsureSettingsInstance(settingsParent);
                UIKit.Stretch((RectTransform)settingsPanel.transform);
                settingsPanel.transform.SetAsLastSibling();
                if (settingButton != null) UIKit.AddClick(settingButton.GetComponent<Button>(), settingsPanel.Open);
            }

            // 게임 오버 창은 화면 전체를 덮도록 루트를 늘립니다(프리팹 루트 RectTransform은 씬 값이 우선).
            GameObject gameOverUi = Find("GameOver_UI");
            if (gameOverUi != null)
            {
                UIKit.Stretch((RectTransform)gameOverUi.transform);
                // 씬에 남아 있던 예전 자식 참조 오버라이드(지금은 비어 있음)를 되돌려 프리팹의 새 창·버튼을 쓰게 합니다.
                UIGameOver gameOverView = gameOverUi.GetComponent<UIGameOver>();
                if (gameOverView != null) RevertFields(gameOverView, "_gameOverUI", "_window", "_subtitleText", "_retryButton", "_titleButton");
            }

            GameObject tablet = Find("Tablet_Frame_UI");
            if (tablet != null)
            {
                Transform layout = tablet.transform.Find("Layout");
                if (layout != null && PrefabUtility.IsPartOfPrefabInstance(layout)) PrefabUtility.RevertObjectOverride(layout, InteractionMode.AutomatedAction);
                Transform menu = layout != null ? layout.Find("Tablet_Menu_UI") : null;
                if (menu != null) menu.gameObject.SetActive(false);
            }

            // 정비/전투 UI 묶음은 UIManager가 Phase에 맞춰 켭니다. 꺼진 상태로 저장해야
            // 상점이 GameManager의 정비 시작(진열 초기화)보다 먼저 열려 진열이 어긋나는 일이 없습니다.
            Find("Preparation_UIs")?.SetActive(false);
            Find("Combat_UIs")?.SetActive(false);

            // 메인 씬에서는 시작 시 테스트용으로 지급하던 아이템(낙뢰·전류 탄환)을 없애 빈 손으로 시작합니다.
            if (scenePath == MainScenePath)
            {
                foreach (Component tester in all.Where(t => t != null).Select(t => t.GetComponent("ItemPipelineTester")).Where(c => c != null))
                {
                    SerializedObject so = new SerializedObject(tester);
                    so.FindProperty("itemIds").arraySize = 0;
                    so.FindProperty("equipOnStart").boolValue = false;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GameObject canvas = Find("Preparation_UIs")?.transform.parent.gameObject ?? Find("Canvas");
            GameObject result = Find("GameResult_UI");
            if (result == null && canvas != null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);
                result = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            }
            if (result != null)
            {
                result.transform.SetAsLastSibling();
                UIKit.Stretch((RectTransform)result.transform);
                result.SetActive(false);
            }

            UIManager uiManager = FindComp<UIManager>();
            if (uiManager != null && result != null)
            {
                UIKit.SetRef(uiManager, "_gameClearView", result.GetComponent<UIGameResult>());
                GameObject[] hide = new[] { Find("Preparation_UIs"), Find("Combat_UIs") }.Where(g => g != null).ToArray();
                SerializedObject so = new SerializedObject(uiManager);
                SerializedProperty hideProp = so.FindProperty("_gameClear").FindPropertyRelative("_hide");
                hideProp.arraySize = hide.Length;
                for (int i = 0; i < hide.Length; i++) hideProp.GetArrayElementAtIndex(i).objectReferenceValue = hide[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LowPolyUIBuilder] HUD 배치/연결 완료: {scenePath}");
        }

        private static readonly string[] HudNames = { "Player_Items_UI", "Player_Skills_UI", "Player_Gold_UI", "Wave_RemainingEnemy_UI", "Player_HP_UI" };

        /// <summary>
        /// HUD 프리팹 인스턴스 안에 씬에서 직접 추가된(프리팹에 없는) 오브젝트 목록을 반환합니다. remove=true면 삭제 후 씬을 저장합니다.
        /// 예전 레이아웃용 라벨/구분선이 새 프리팹 위에 겹쳐 보이는 것을 정리할 때 사용합니다.
        /// </summary>
        public static string CleanAddedHudObjects(string scenePath, bool remove)
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.isDirty && active.path != scenePath) return "ERR active scene dirty";
            Scene scene = active.path == scenePath ? active : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Transform[] all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            System.Collections.Generic.List<string> found = new System.Collections.Generic.List<string>();
            foreach (string hud in HudNames)
            {
                GameObject root = all.FirstOrDefault(t => t != null && t.name == hud)?.gameObject;
                if (root == null || !PrefabUtility.IsAnyPrefabInstanceRoot(root)) continue;
                foreach (var added in PrefabUtility.GetAddedGameObjects(root))
                {
                    GameObject go = added.instanceGameObject;
                    if (go == null) continue;
                    found.Add($"{hud}/{go.name}");
                    if (remove) Object.DestroyImmediate(go);
                }
            }
            if (remove && found.Count > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            return found.Count == 0 ? "(none)" : string.Join(", ", found);
        }

        // ================================================================== Helpers

        private static void EditPrefab(string path, Action<GameObject> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>씬 인스턴스에 남아 있을 수 있는 자식 참조 오버라이드를 되돌려 프리팹 값을 사용하게 합니다.</summary>
        private static void RevertFields(Object component, params string[] fields)
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(component)) return;
            SerializedObject so = new SerializedObject(component);
            foreach (string field in fields)
            {
                SerializedProperty prop = so.FindProperty(field);
                if (prop != null && prop.prefabOverride) PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
            }
        }
    }
}
