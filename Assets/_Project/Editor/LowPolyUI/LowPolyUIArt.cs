using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace UJam.EditorTools.LowPolyUI
{
    /// <summary>
    /// 게임의 로우폴리 스타일에 맞춘 UI 스프라이트(패널, 공용 정사각 슬롯, 버튼, 아이콘)를 코드로 생성합니다.
    /// 결과 PNG는 Assets/_Project/Art/UI/LowPoly에 저장되며 9-slice 테두리까지 설정됩니다.
    /// 아티스트 리소스로 교체할 때는 같은 이름의 PNG를 덮어쓰면 프리팹 참조가 유지됩니다.
    /// </summary>
    internal static class LowPolyUIArt
    {
        public const string Dir = "Assets/_Project/Art/UI/LowPoly";
        public const string FontPath = "Assets/_Project/Art/Font/MALGUNBD SDF.asset";
        public const string FontShadowMatPath = "Assets/_Project/Art/Font/MALGUNBD SDF Shadow.mat";

        // ---- 팔레트: 어두운 석판 + 청동 테두리 + 주황/금색 포인트 ----
        public static readonly Color Outline = LowPolyCanvas.Hex("#15131C");
        public static readonly Color BodyDark = LowPolyCanvas.Hex("#25222F");
        public static readonly Color Body = LowPolyCanvas.Hex("#2E2B3B");
        public static readonly Color BodyLight = LowPolyCanvas.Hex("#3A3649");
        public static readonly Color BronzeDark = LowPolyCanvas.Hex("#4A3426");
        public static readonly Color Bronze = LowPolyCanvas.Hex("#9A7250");
        public static readonly Color BronzeLight = LowPolyCanvas.Hex("#E2BF8A");
        public static readonly Color StoneDark = LowPolyCanvas.Hex("#191723");
        public static readonly Color Stone = LowPolyCanvas.Hex("#3F3A4F");
        public static readonly Color StoneLight = LowPolyCanvas.Hex("#6D6585");

        [MenuItem("Tools/UJam/UI/1. Generate LowPoly Art", priority = 1)]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Dir);
            Panel();
            Header();
            Slot();
            Button();
            Coin();
            Enemy();
            Heart();
            Medallion();
            HpFrame();
            HpFill();
            Bag();
            Bolt();
            Clock();
            Lock();
            Back();
            Pause();
            Divider();
            Banner();
            White();
            EnsureFont();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LowPolyUIArt] 로우폴리 UI 스프라이트 생성 완료: " + Dir);
        }

        public static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Dir}/{name}.png");

        // ------------------------------------------------------------------ Panels

        /// <summary>메인 패널: 깎인 모서리, 청동 면 테두리, 미세하게 면이 나뉜 석판 몸체, 모서리 리벳.</summary>
        private static void Panel()
        {
            const int S = 192;
            const float cut = 22f, rim = 14f;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] outer = LowPolyCanvas.Chamfer(2, 2, S - 4, S - 4, cut);
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 2f));
            FacetBody(c, 2 + rim, 2 + rim, S - 4 - rim * 2, S - 4 - rim * 2, BodyDark, Body, BodyLight, 4, 1234);
            Vector2[] inner = LowPolyCanvas.ChamferInset(2, 2, S - 4, S - 4, cut, rim);
            c.Rim(outer, inner, BronzeDark, Bronze, BronzeLight, true);
            // 테두리 안쪽의 얕은 홈(오목) 표현
            Vector2[] groove = LowPolyCanvas.ChamferInset(2, 2, S - 4, S - 4, cut, rim + 3f);
            c.Rim(inner, groove, StoneDark, Stone, StoneLight, false);
            // 모서리 리벳
            float r = 2 + cut * 0.5f + rim * 0.35f;
            Rivet(c, new Vector2(r, r), 5.5f);
            Rivet(c, new Vector2(S - r, r), 5.5f);
            Rivet(c, new Vector2(r, S - r), 5.5f);
            Rivet(c, new Vector2(S - r, S - r), 5.5f);
            Save(c, "LP_Panel", new Vector4(44, 44, 44, 44));
        }

        /// <summary>패널 제목판: 양 끝이 뾰족한 육각 띠.</summary>
        private static void Header()
        {
            const int W = 256, H = 72;
            const float e = 28f, rim = 6f;
            LowPolyCanvas c = new LowPolyCanvas(W, H);
            Vector2[] outer =
            {
                new Vector2(e, 3), new Vector2(W - e, 3), new Vector2(W - 3, H * 0.5f),
                new Vector2(W - e, H - 3), new Vector2(e, H - 3), new Vector2(3, H * 0.5f),
            };
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 2.5f));
            Vector2[] inner = LowPolyCanvas.Offset(outer, -rim);
            c.Poly(LowPolyCanvas.Hex("#211E2C"), inner);
            // 몸체 상하 2면
            c.Poly(LowPolyCanvas.Hex("#2B2739"), inner[5], inner[0], inner[1], inner[2]);
            c.Rim(outer, inner, BronzeDark, Bronze, BronzeLight, true);
            Save(c, "LP_Header", new Vector4(36, 24, 36, 24));
        }

        /// <summary>공용 정사각 슬롯 프레임(오목): 모든 아이템/스킬 아이콘이 이 프레임을 공유합니다.</summary>
        private static void Slot()
        {
            const int S = 128;
            const float cut = 12f, rim = 11f;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] outer = LowPolyCanvas.Chamfer(3, 3, S - 6, S - 6, cut);
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 2.5f));
            Vector2[] inner = LowPolyCanvas.ChamferInset(3, 3, S - 6, S - 6, cut, rim);
            Vector2 center = new Vector2(S * 0.5f, S * 0.5f);
            c.Poly(LowPolyCanvas.Hex("#211E2C"), inner);
            c.FacetFan(inner, center + new Vector2(6, 8), LowPolyCanvas.Hex("#1A1824"), LowPolyCanvas.Hex("#221F2E"), LowPolyCanvas.Hex("#2C2839"), -0.5f);
            c.Rim(outer, inner, StoneDark, Stone, StoneLight, false);
            // 바깥 청동 테 얇은 라인
            Vector2[] lip = LowPolyCanvas.ChamferInset(3, 3, S - 6, S - 6, cut, 3.5f);
            c.Rim(outer, lip, BronzeDark, Bronze, BronzeLight, true);
            Save(c, "LP_Slot", new Vector4(26, 26, 26, 26));
        }

        /// <summary>회색조 버튼(색은 Image.color로 입힘): 볼록한 면 테두리 + 비스듬히 나뉜 2면 몸체.</summary>
        private static void Button()
        {
            const int W = 160, H = 72;
            const float cut = 14f, rim = 8f;
            LowPolyCanvas c = new LowPolyCanvas(W, H);
            Vector2[] outer = LowPolyCanvas.Chamfer(3, 3, W - 6, H - 6, cut);
            c.Poly(LowPolyCanvas.Hex("#202020"), LowPolyCanvas.Offset(outer, 2.5f));
            Vector2[] inner = LowPolyCanvas.ChamferInset(3, 3, W - 6, H - 6, cut, rim);
            c.Poly(LowPolyCanvas.Hex("#D9D9D9"), inner);
            // 윗면(밝음) - 비스듬한 분할선
            c.Poly(LowPolyCanvas.Hex("#F4F4F4"), inner[7], inner[0], inner[1], inner[2], new Vector2(inner[2].x, H * 0.46f), new Vector2(inner[7].x, H * 0.56f));
            c.Rim(outer, inner, LowPolyCanvas.Hex("#6E6E6E"), LowPolyCanvas.Hex("#BDBDBD"), Color.white, true);
            Save(c, "LP_Button", new Vector4(28, 28, 28, 28));
        }

        /// <summary>얇은 구분선: 양 끝 다이아몬드.</summary>
        private static void Divider()
        {
            const int W = 256, H = 24;
            LowPolyCanvas c = new LowPolyCanvas(W, H);
            c.Poly(Outline, new Vector2(14, 9), new Vector2(W - 14, 9), new Vector2(W - 14, 15), new Vector2(14, 15));
            c.Poly(Bronze, new Vector2(14, 10.5f), new Vector2(W - 14, 10.5f), new Vector2(W - 14, 12), new Vector2(14, 12));
            c.Poly(BronzeDark, new Vector2(14, 12), new Vector2(W - 14, 12), new Vector2(W - 14, 13.5f), new Vector2(14, 13.5f));
            Diamond(c, new Vector2(13, 12), 9f, BronzeDark, Bronze, BronzeLight);
            Diamond(c, new Vector2(W - 13, 12), 9f, BronzeDark, Bronze, BronzeLight);
            Save(c, "LP_Divider", new Vector4(28, 0, 28, 0));
        }

        /// <summary>결산 창 제목 리본: 주황/금 육각 띠 + 뒤로 접힌 꼬리.</summary>
        private static void Banner()
        {
            const int W = 512, H = 144;
            LowPolyCanvas c = new LowPolyCanvas(W, H);
            Color tailDark = LowPolyCanvas.Hex("#7A3A10"), tail = LowPolyCanvas.Hex("#B5581A"), tailLight = LowPolyCanvas.Hex("#D9792C");
            // 꼬리(뒤)
            Vector2[] lt = { new Vector2(4, 46), new Vector2(84, 46), new Vector2(84, 134), new Vector2(4, 134), new Vector2(30, 90) };
            Vector2[] rt = { new Vector2(W - 4, 46), new Vector2(W - 4, 134), new Vector2(W - 84, 134), new Vector2(W - 84, 46), new Vector2(W - 30, 90) };
            c.Poly(Outline, LowPolyCanvas.Offset(lt, 2.5f));
            c.Poly(Outline, LowPolyCanvas.Offset(rt, 2.5f));
            c.FacetFan(lt, new Vector2(56, 90), tailDark, tail, tailLight, 0.7f);
            c.FacetFan(rt, new Vector2(W - 56, 90), tailDark, tail, tailLight, 0.7f);
            // 접힌 부분 그림자
            c.Poly(LowPolyCanvas.Hex("#4A200A"), new Vector2(66, 118), new Vector2(84, 134), new Vector2(84, 118));
            c.Poly(LowPolyCanvas.Hex("#4A200A"), new Vector2(W - 66, 118), new Vector2(W - 84, 118), new Vector2(W - 84, 134));
            // 본판
            Vector2[] outer =
            {
                new Vector2(70, 12), new Vector2(W - 70, 12), new Vector2(W - 40, 65),
                new Vector2(W - 70, 118), new Vector2(70, 118), new Vector2(40, 65),
            };
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 3f));
            Vector2[] inner = LowPolyCanvas.Offset(outer, -10f);
            c.Poly(LowPolyCanvas.Hex("#E7862A"), inner);
            c.Poly(LowPolyCanvas.Hex("#F6A23E"), inner[5], inner[0], inner[1], inner[2], new Vector2(inner[2].x - 6, 60), new Vector2(inner[5].x + 6, 70));
            c.Rim(outer, inner, LowPolyCanvas.Hex("#8C4512"), LowPolyCanvas.Hex("#E08A2E"), LowPolyCanvas.Hex("#FFD27A"), true);
            Save(c, "LP_Banner", new Vector4(112, 40, 112, 40));
        }

        /// <summary>단색 채움(dim 배경, 구분선, 쿨타임 오버레이)에 쓰는 흰색 정사각 스프라이트.</summary>
        private static void White()
        {
            LowPolyCanvas c = new LowPolyCanvas(8, 8, 1);
            c.Poly(Color.white, new Vector2(0, 0), new Vector2(8, 0), new Vector2(8, 8), new Vector2(0, 8));
            Save(c, "LP_White", Vector4.zero);
        }

        // ------------------------------------------------------------------ Icons

        /// <summary>로우폴리 금화: 10각 테두리 면 + 중앙 면 분할 + 각인 + 옆면 두께.</summary>
        private static void Coin()
        {
            const int S = 128;
            Vector2 center = new Vector2(62, 62);
            Color dark = LowPolyCanvas.Hex("#8E5410"), mid = LowPolyCanvas.Hex("#E39B22"), light = LowPolyCanvas.Hex("#FFE58A");
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] side = LowPolyCanvas.Regular(center + new Vector2(5, 4), 54f, 10, -90f);
            Vector2[] outer = LowPolyCanvas.Regular(center, 54f, 10, -90f);
            c.Poly(LowPolyCanvas.Hex("#4A2A06"), LowPolyCanvas.Offset(side, 3f));
            c.Poly(LowPolyCanvas.Hex("#4A2A06"), LowPolyCanvas.Offset(outer, 3f));
            c.Poly(LowPolyCanvas.Hex("#9C5E14"), side);
            Vector2[] innerRing = LowPolyCanvas.Regular(center, 41f, 10, -90f);
            c.Rim(outer, innerRing, dark, mid, light, true);
            c.FacetFan(innerRing, center, dark, LowPolyCanvas.Hex("#F2B437"), light, 0.35f);
            // 각인(오목한 다이아몬드)
            Vector2[] emblem = LowPolyCanvas.Regular(center, 17f, 4, -90f);
            Vector2[] emblemIn = LowPolyCanvas.Regular(center, 9f, 4, -90f);
            c.Rim(emblem, emblemIn, dark, mid, light, false);
            c.Poly(LowPolyCanvas.Hex("#F7C04A"), emblemIn);
            // 하이라이트 면
            c.Tri(new Color(1f, 1f, 1f, 0.35f), center + new Vector2(-30, -20), center + new Vector2(-18, -32), center + new Vector2(-4, -26));
            Save(c, "LP_Coin", Vector4.zero);
        }

        /// <summary>남은 적 아이콘: 레퍼런스(붉은 귀 달린 방울 형태)를 면 분할한 로우폴리 버전.</summary>
        private static void Enemy()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] body =
            {
                new Vector2(40, 10), new Vector2(55, 33), new Vector2(73, 33), new Vector2(88, 10),
                new Vector2(99, 40), new Vector2(108, 63), new Vector2(107, 86), new Vector2(97, 105),
                new Vector2(80, 117), new Vector2(64, 120), new Vector2(48, 117), new Vector2(31, 105),
                new Vector2(21, 86), new Vector2(20, 63), new Vector2(29, 40),
            };
            c.Poly(LowPolyCanvas.Hex("#3A0D08"), LowPolyCanvas.Offset(body, 3.5f));
            c.FacetFan(body, new Vector2(58, 66), LowPolyCanvas.Hex("#9E2414"), LowPolyCanvas.Hex("#F04A2E"), LowPolyCanvas.Hex("#FF9466"), 0.75f);
            // 귀 안쪽 면
            c.Tri(LowPolyCanvas.Hex("#C9321C"), new Vector2(41, 17), new Vector2(51, 33), new Vector2(33, 38));
            c.Tri(LowPolyCanvas.Hex("#B02A18"), new Vector2(87, 17), new Vector2(95, 38), new Vector2(77, 33));
            // 눈
            Vector2[] leftEye = { new Vector2(37, 72), new Vector2(52, 70), new Vector2(58, 77), new Vector2(55, 87), new Vector2(42, 88), new Vector2(36, 81) };
            Vector2[] rightEye = { new Vector2(70, 77), new Vector2(76, 70), new Vector2(91, 72), new Vector2(92, 81), new Vector2(86, 88), new Vector2(73, 87) };
            c.Poly(LowPolyCanvas.Hex("#160A0C"), leftEye);
            c.Poly(LowPolyCanvas.Hex("#160A0C"), rightEye);
            c.Tri(new Color(1f, 1f, 1f, 0.75f), new Vector2(42, 74), new Vector2(48, 73), new Vector2(43, 79));
            c.Tri(new Color(1f, 1f, 1f, 0.75f), new Vector2(76, 74), new Vector2(82, 73), new Vector2(77, 79));
            Save(c, "LP_Enemy", Vector4.zero);
        }

        /// <summary>면 분할된 하트(HP 표시용, 'HP' 문구 없이 사용).</summary>
        private static void Heart()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            const int n = 18;
            Vector2[] pts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = Mathf.PI * 2f * i / n;
                float x = 16f * Mathf.Pow(Mathf.Sin(t), 3f);
                float y = 13f * Mathf.Cos(t) - 5f * Mathf.Cos(2f * t) - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                pts[i] = new Vector2(64f + x * 3.3f, 60f - y * 3.3f);
            }
            c.Poly(LowPolyCanvas.Hex("#3A0A12"), LowPolyCanvas.Offset(pts, 3.5f));
            c.FacetFan(pts, new Vector2(56, 52), LowPolyCanvas.Hex("#8C1A2C"), LowPolyCanvas.Hex("#E3344C"), LowPolyCanvas.Hex("#FF8C96"), 0.8f);
            c.Tri(new Color(1f, 1f, 1f, 0.55f), new Vector2(34, 36), new Vector2(46, 28), new Vector2(40, 46));
            Save(c, "LP_Heart", Vector4.zero);
        }

        /// <summary>HP 게이지 위의 육각 메달(하트 받침).</summary>
        private static void Medallion()
        {
            const int S = 160;
            Vector2 center = new Vector2(80, 80);
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] outer = LowPolyCanvas.Regular(center, 74f, 6, -90f);
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 3f));
            Vector2[] inner = LowPolyCanvas.Regular(center, 58f, 6, -90f);
            c.Rim(outer, inner, BronzeDark, Bronze, BronzeLight, true);
            Vector2[] well = LowPolyCanvas.Regular(center, 52f, 6, -90f);
            c.Rim(inner, well, StoneDark, Stone, StoneLight, false);
            c.FacetFan(well, center, LowPolyCanvas.Hex("#1E1A2A"), LowPolyCanvas.Hex("#2A2440"), LowPolyCanvas.Hex("#3A3356"), -0.4f);
            Save(c, "LP_Medallion", Vector4.zero);
        }

        /// <summary>세로 HP 게이지 프레임: 위는 깎인 모서리, 아래는 뾰족한 끝. 안쪽은 빈 게이지 색.</summary>
        private static void HpFrame()
        {
            const int W = 112, H = 320;
            const float cut = 18f, tip = 34f, rim = 12f;
            LowPolyCanvas c = new LowPolyCanvas(W, H);
            Vector2[] outer =
            {
                new Vector2(cut, 3), new Vector2(W - cut, 3), new Vector2(W - 3, cut),
                new Vector2(W - 3, H - tip), new Vector2(W * 0.5f, H - 3), new Vector2(3, H - tip), new Vector2(3, cut),
            };
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 2.5f));
            Vector2[] inner = LowPolyCanvas.Offset(outer, -rim);
            c.Poly(LowPolyCanvas.Hex("#2A2440"), inner);
            // 빈 게이지 면 분할(좌 밝음/우 어두움)
            c.Poly(LowPolyCanvas.Hex("#332C4E"), inner[6], inner[0], new Vector2(W * 0.42f, inner[0].y), new Vector2(W * 0.38f, H * 0.5f), new Vector2(W * 0.42f, inner[4].y - 14), inner[5]);
            c.Poly(LowPolyCanvas.Hex("#211C33"), inner[1], inner[2], inner[3], new Vector2(W * 0.62f, inner[4].y - 16), new Vector2(W * 0.66f, H * 0.5f), new Vector2(W * 0.62f, inner[1].y));
            c.Rim(outer, inner, BronzeDark, Bronze, BronzeLight, true);
            Vector2[] groove = LowPolyCanvas.Offset(inner, -3f);
            c.Rim(inner, groove, StoneDark, Stone, StoneLight, false);
            Save(c, "LP_HPFrame", new Vector4(40, 60, 40, 40));
        }

        /// <summary>HP 게이지 채움(세로 Filled 이미지): 좌/중/우 지그재그 면.</summary>
        private static void HpFill()
        {
            const int W = 64, H = 256;
            LowPolyCanvas c = new LowPolyCanvas(W, H, 2);
            c.Poly(LowPolyCanvas.Hex("#E3364C"), new Vector2(0, 0), new Vector2(W, 0), new Vector2(W, H), new Vector2(0, H));
            c.Poly(LowPolyCanvas.Hex("#FF7B88"), new Vector2(0, 0), new Vector2(22, 0), new Vector2(15, 64), new Vector2(23, 128), new Vector2(15, 192), new Vector2(22, H), new Vector2(0, H));
            c.Poly(LowPolyCanvas.Hex("#FFB0B6"), new Vector2(0, 0), new Vector2(8, 0), new Vector2(5, 128), new Vector2(8, H), new Vector2(0, H));
            c.Poly(LowPolyCanvas.Hex("#A91F38"), new Vector2(W, 0), new Vector2(46, 0), new Vector2(53, 64), new Vector2(44, 128), new Vector2(53, 192), new Vector2(46, H), new Vector2(W, H));
            c.Poly(LowPolyCanvas.Hex("#F04F63"), new Vector2(22, 0), new Vector2(46, 0), new Vector2(34, 32));
            c.Poly(LowPolyCanvas.Hex("#C92A42"), new Vector2(23, 128), new Vector2(44, 128), new Vector2(34, 160));
            Save(c, "LP_HPFill", Vector4.zero);
        }

        /// <summary>아이템 목록 제목용 주머니 아이콘.</summary>
        private static void Bag()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Color dark = LowPolyCanvas.Hex("#8A6A42"), mid = LowPolyCanvas.Hex("#D9BC8C"), light = LowPolyCanvas.Hex("#FFF0CF");
            Vector2[] body = { new Vector2(36, 50), new Vector2(92, 50), new Vector2(110, 84), new Vector2(104, 112), new Vector2(84, 120), new Vector2(44, 120), new Vector2(24, 112), new Vector2(18, 84) };
            Vector2[] neck = { new Vector2(46, 30), new Vector2(82, 30), new Vector2(90, 52), new Vector2(38, 52) };
            Vector2[] top = { new Vector2(44, 8), new Vector2(64, 20), new Vector2(84, 8), new Vector2(86, 32), new Vector2(42, 32) };
            c.Poly(LowPolyCanvas.Hex("#2E1F10"), LowPolyCanvas.Offset(body, 3.5f));
            c.Poly(LowPolyCanvas.Hex("#2E1F10"), LowPolyCanvas.Offset(neck, 3.5f));
            c.Poly(LowPolyCanvas.Hex("#2E1F10"), LowPolyCanvas.Offset(top, 3.5f));
            c.FacetFan(top, new Vector2(64, 24), dark, mid, light, 0.7f);
            c.FacetFan(body, new Vector2(58, 80), dark, mid, light, 0.7f);
            c.FacetFan(neck, new Vector2(64, 42), dark, mid, light, 0.5f);
            // 끈
            c.Poly(LowPolyCanvas.Hex("#9C4A22"), new Vector2(40, 42), new Vector2(88, 42), new Vector2(90, 52), new Vector2(38, 52));
            c.Poly(LowPolyCanvas.Hex("#C9682F"), new Vector2(40, 42), new Vector2(88, 42), new Vector2(89, 46), new Vector2(39, 46));
            Save(c, "LP_Bag", Vector4.zero);
        }

        /// <summary>사용할 아이템/스킬 제목용 번개 아이콘.</summary>
        private static void Bolt()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] bolt = { new Vector2(74, 6), new Vector2(28, 72), new Vector2(58, 72), new Vector2(44, 122), new Vector2(100, 50), new Vector2(68, 50), new Vector2(88, 6) };
            c.Poly(LowPolyCanvas.Hex("#5A3A08"), LowPolyCanvas.Offset(bolt, 3.5f));
            c.Poly(LowPolyCanvas.Hex("#F2A61E"), bolt);
            c.Poly(LowPolyCanvas.Hex("#FFE27A"), bolt[0], bolt[1], new Vector2(58, 72), new Vector2(70, 46), bolt[6]);
            c.Poly(LowPolyCanvas.Hex("#FFF6C2"), bolt[0], new Vector2(46, 62), new Vector2(60, 46), bolt[6]);
            c.Tri(LowPolyCanvas.Hex("#D9830F"), new Vector2(58, 72), bolt[3], new Vector2(64, 62));
            Save(c, "LP_Bolt", Vector4.zero);
        }

        /// <summary>결산 창의 시간 아이콘.</summary>
        private static void Clock()
        {
            const int S = 128;
            Vector2 center = new Vector2(64, 66);
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] outer = LowPolyCanvas.Regular(center, 54f, 8, -112.5f);
            c.Poly(Outline, LowPolyCanvas.Offset(outer, 3f));
            Vector2[] inner = LowPolyCanvas.Regular(center, 42f, 8, -112.5f);
            c.Rim(outer, inner, BronzeDark, Bronze, BronzeLight, true);
            c.FacetFan(inner, center, LowPolyCanvas.Hex("#BFB3A0"), LowPolyCanvas.Hex("#EDE3D2"), Color.white, -0.35f);
            // 바늘
            c.Poly(LowPolyCanvas.Hex("#2A2433"), center + new Vector2(-4, 2), center + new Vector2(0, -34), center + new Vector2(4, 2));
            c.Poly(LowPolyCanvas.Hex("#E2552E"), center + new Vector2(-3, -3), center + new Vector2(26, 12), center + new Vector2(-1, 4));
            c.Poly(LowPolyCanvas.Hex("#2A2433"), LowPolyCanvas.Regular(center, 6f, 6, 0f));
            // 위 손잡이
            c.Poly(Outline, new Vector2(52, 2), new Vector2(76, 2), new Vector2(72, 14), new Vector2(56, 14));
            c.Poly(Bronze, new Vector2(55, 4), new Vector2(73, 4), new Vector2(70, 12), new Vector2(58, 12));
            Save(c, "LP_Clock", Vector4.zero);
        }

        /// <summary>잠긴 레벨/잠긴 슬롯용 자물쇠.</summary>
        private static void Lock()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            // 고리(두꺼운 U자)
            Vector2[] shackle =
            {
                new Vector2(34, 60), new Vector2(34, 36), new Vector2(46, 16), new Vector2(64, 10), new Vector2(82, 16), new Vector2(94, 36), new Vector2(94, 60),
                new Vector2(80, 60), new Vector2(80, 40), new Vector2(74, 28), new Vector2(64, 24), new Vector2(54, 28), new Vector2(48, 40), new Vector2(48, 60),
            };
            c.Poly(Outline, LowPolyCanvas.Offset(shackle, 3f));
            c.Poly(LowPolyCanvas.Hex("#8F95A8"), shackle);
            c.Poly(LowPolyCanvas.Hex("#C9CEDB"), shackle[0], shackle[1], shackle[2], shackle[3], shackle[10], shackle[11], shackle[12], shackle[13]);
            Vector2[] body = LowPolyCanvas.Chamfer(22, 54, 84, 64, 10);
            c.Poly(Outline, LowPolyCanvas.Offset(body, 3f));
            Vector2[] bodyIn = LowPolyCanvas.ChamferInset(22, 54, 84, 64, 10, 8);
            c.Poly(LowPolyCanvas.Hex("#D9A441"), bodyIn);
            c.FacetFan(bodyIn, new Vector2(60, 82), LowPolyCanvas.Hex("#9A6A1C"), LowPolyCanvas.Hex("#D9A441"), LowPolyCanvas.Hex("#FFE08A"), 0.4f);
            c.Rim(body, bodyIn, LowPolyCanvas.Hex("#7A4E12"), LowPolyCanvas.Hex("#C08A2C"), LowPolyCanvas.Hex("#FFD76E"), true);
            c.Poly(LowPolyCanvas.Hex("#2A1A08"), LowPolyCanvas.Regular(new Vector2(64, 80), 7f, 6, -90f));
            c.Poly(LowPolyCanvas.Hex("#2A1A08"), new Vector2(61, 84), new Vector2(67, 84), new Vector2(69, 100), new Vector2(59, 100));
            Save(c, "LP_Lock", Vector4.zero);
        }

        /// <summary>뒤로 가기 화살표(회색조, 색 입힘 가능).</summary>
        private static void Back()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            Vector2[] arrow = { new Vector2(74, 16), new Vector2(98, 40), new Vector2(66, 64), new Vector2(98, 88), new Vector2(74, 112), new Vector2(26, 64) };
            c.Poly(LowPolyCanvas.Hex("#202020"), LowPolyCanvas.Offset(arrow, 3.5f));
            c.FacetFan(arrow, new Vector2(60, 60), LowPolyCanvas.Hex("#8A8A8A"), LowPolyCanvas.Hex("#D6D6D6"), Color.white, 0.7f);
            Save(c, "LP_Back", Vector4.zero);
        }

        /// <summary>일시정지 아이콘(회색조, 색 입힘 가능): 면이 나뉜 두 막대.</summary>
        private static void Pause()
        {
            const int S = 128;
            LowPolyCanvas c = new LowPolyCanvas(S, S);
            foreach (float x in new[] { 26f, 72f })
            {
                Vector2[] bar = LowPolyCanvas.Chamfer(x, 18, 30, 92, 8);
                c.Poly(LowPolyCanvas.Hex("#202020"), LowPolyCanvas.Offset(bar, 3.5f));
                c.FacetFan(bar, new Vector2(x + 12, 56), LowPolyCanvas.Hex("#8A8A8A"), LowPolyCanvas.Hex("#D6D6D6"), Color.white, 0.7f);
            }
            Save(c, "LP_Pause", Vector4.zero);
        }

        // ------------------------------------------------------------------ Helpers

        /// <summary>격자 점을 흔들어 삼각형으로 나눈 몸체 면을 칠합니다(로우폴리 표면).</summary>
        private static void FacetBody(LowPolyCanvas c, float x, float y, float w, float h, Color dark, Color mid, Color light, int cells, int seed)
        {
            System.Random rng = new System.Random(seed);
            Vector2[,] grid = new Vector2[cells + 1, cells + 1];
            for (int gy = 0; gy <= cells; gy++)
            {
                for (int gx = 0; gx <= cells; gx++)
                {
                    float jx = (gx == 0 || gx == cells) ? 0f : ((float)rng.NextDouble() - 0.5f) * (w / cells) * 0.55f;
                    float jy = (gy == 0 || gy == cells) ? 0f : ((float)rng.NextDouble() - 0.5f) * (h / cells) * 0.55f;
                    grid[gx, gy] = new Vector2(x + w * gx / cells + jx, y + h * gy / cells + jy);
                }
            }
            for (int gy = 0; gy < cells; gy++)
            {
                for (int gx = 0; gx < cells; gx++)
                {
                    Vector2 a = grid[gx, gy], b = grid[gx + 1, gy], d = grid[gx + 1, gy + 1], e = grid[gx, gy + 1];
                    bool flip = ((gx + gy) & 1) == 0;
                    float s1 = ((float)rng.NextDouble() - 0.5f) * 0.5f + 0.08f;
                    float s2 = ((float)rng.NextDouble() - 0.5f) * 0.5f - 0.08f;
                    if (flip)
                    {
                        c.Tri(LowPolyCanvas.Shade(dark, mid, light, s1), a, b, d);
                        c.Tri(LowPolyCanvas.Shade(dark, mid, light, s2), a, d, e);
                    }
                    else
                    {
                        c.Tri(LowPolyCanvas.Shade(dark, mid, light, s1), a, b, e);
                        c.Tri(LowPolyCanvas.Shade(dark, mid, light, s2), b, d, e);
                    }
                }
            }
        }

        private static void Rivet(LowPolyCanvas c, Vector2 p, float r)
        {
            c.Poly(Outline, LowPolyCanvas.Regular(p, r + 1.5f, 4, -90f));
            Diamond(c, p, r, BronzeDark, LowPolyCanvas.Hex("#C59A66"), LowPolyCanvas.Hex("#FFE6B8"));
        }

        private static void Diamond(LowPolyCanvas c, Vector2 p, float r, Color dark, Color mid, Color light)
        {
            Vector2 top = p + new Vector2(0, -r), right = p + new Vector2(r, 0), bottom = p + new Vector2(0, r), left = p + new Vector2(-r, 0);
            c.Tri(LowPolyCanvas.Shade(dark, mid, light, 0.9f), left, top, p);
            c.Tri(LowPolyCanvas.Shade(dark, mid, light, 0.3f), top, right, p);
            c.Tri(LowPolyCanvas.Shade(dark, mid, light, -0.9f), right, bottom, p);
            c.Tri(LowPolyCanvas.Shade(dark, mid, light, -0.3f), bottom, left, p);
        }

        /// <summary>PNG 저장 후 Sprite(9-slice 테두리 포함)로 임포트합니다.</summary>
        private static void Save(LowPolyCanvas canvas, string name, Vector4 border)
        {
            string path = $"{Dir}/{name}.png";
            Texture2D tex = canvas.ToTexture();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        /// <summary>
        /// 로우폴리 UI에 어울리는 굵은 고딕(맑은 고딕 Bold) 동적 SDF 폰트와 그림자 머티리얼을 준비합니다.
        /// 한글은 사용 시점에 동적으로 아틀라스에 추가됩니다.
        /// </summary>
        public static TMP_FontAsset EnsureFont()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (fontAsset == null)
            {
                Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/Art/Font/MALGUNBD.TTF");
                fontAsset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                fontAsset.name = "MALGUNBD SDF";
                AssetDatabase.CreateAsset(fontAsset, FontPath);
                fontAsset.atlasTextures[0].name = "MALGUNBD SDF Atlas";
                AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
                fontAsset.material.name = "MALGUNBD SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
                AssetDatabase.SaveAssets();
            }

            if (AssetDatabase.LoadAssetAtPath<Material>(FontShadowMatPath) == null)
            {
                Material shadow = new Material(fontAsset.material) { name = "MALGUNBD SDF Shadow" };
                shadow.EnableKeyword("UNDERLAY_ON");
                shadow.SetColor("_UnderlayColor", new Color(0.05f, 0.03f, 0.08f, 0.85f));
                shadow.SetFloat("_UnderlayOffsetX", 0.7f);
                shadow.SetFloat("_UnderlayOffsetY", -0.7f);
                shadow.SetFloat("_UnderlayDilate", 0.25f);
                shadow.SetFloat("_UnderlaySoftness", 0.1f);
                AssetDatabase.CreateAsset(shadow, FontShadowMatPath);
            }
            return fontAsset;
        }

        public static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        public static Material FontShadow => AssetDatabase.LoadAssetAtPath<Material>(FontShadowMatPath);
    }
}
