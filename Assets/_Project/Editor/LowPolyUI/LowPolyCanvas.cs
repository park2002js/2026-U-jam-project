using System.Collections.Generic;
using UnityEngine;

namespace UJam.EditorTools.LowPolyUI
{
    /// <summary>
    /// 로우폴리 UI 스프라이트를 그리기 위한 간단한 다각형 래스터라이저입니다.
    /// 좌표는 최종 픽셀 단위이며 원점은 왼쪽 위(y 아래 방향)입니다. 내부적으로 슈퍼샘플링해 가장자리를 부드럽게 합니다.
    /// </summary>
    internal sealed class LowPolyCanvas
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int _ss;
        private readonly Color[] _buffer; // premultiplied alpha, hi-res

        /// <summary>왼쪽 위에서 오른쪽 아래를 향하는 기본 광원 방향(면이 이 방향을 향할수록 밝음).</summary>
        public static readonly Vector2 LightFrom = new Vector2(-0.5f, -1f).normalized;

        public int Width => _width;
        public int Height => _height;

        public LowPolyCanvas(int width, int height, int supersample = 4)
        {
            _width = width;
            _height = height;
            _ss = supersample;
            _buffer = new Color[width * supersample * height * supersample];
        }

        /// <summary>짝수-홀수 규칙으로 다각형을 채웁니다.</summary>
        public void Poly(Color color, IList<Vector2> points)
        {
            if (points == null || points.Count < 3 || color.a <= 0f) return;
            int w = _width * _ss;
            int h = _height * _ss;
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                minY = Mathf.Min(minY, points[i].y * _ss);
                maxY = Mathf.Max(maxY, points[i].y * _ss);
            }

            int y0 = Mathf.Max(0, Mathf.FloorToInt(minY));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(maxY));
            List<float> xs = new List<float>(8);
            Color pre = new Color(color.r * color.a, color.g * color.a, color.b * color.a, color.a);
            float inv = 1f - color.a;

            for (int y = y0; y <= y1; y++)
            {
                float sy = y + 0.5f;
                xs.Clear();
                for (int i = 0; i < points.Count; i++)
                {
                    Vector2 a = points[i] * _ss;
                    Vector2 b = points[(i + 1) % points.Count] * _ss;
                    if ((a.y <= sy && b.y > sy) || (b.y <= sy && a.y > sy))
                    {
                        float t = (sy - a.y) / (b.y - a.y);
                        xs.Add(a.x + t * (b.x - a.x));
                    }
                }
                if (xs.Count < 2) continue;
                xs.Sort();
                for (int k = 0; k + 1 < xs.Count; k += 2)
                {
                    int x0 = Mathf.Max(0, Mathf.CeilToInt(xs[k] - 0.5f));
                    int x1 = Mathf.Min(w - 1, Mathf.FloorToInt(xs[k + 1] - 0.5f));
                    int row = y * w;
                    for (int x = x0; x <= x1; x++)
                    {
                        Color d = _buffer[row + x];
                        _buffer[row + x] = new Color(pre.r + d.r * inv, pre.g + d.g * inv, pre.b + d.b * inv, pre.a + d.a * inv);
                    }
                }
            }
        }

        public void Poly(Color color, params Vector2[] points) => Poly(color, (IList<Vector2>)points);

        /// <summary>삼각형 하나를 채웁니다.</summary>
        public void Tri(Color color, Vector2 a, Vector2 b, Vector2 c) => Poly(color, new[] { a, b, c });

        /// <summary>
        /// 다각형을 기준점에서 부채꼴로 나누고, 각 면의 기울기를 광원과 비교해 명암을 줍니다.
        /// tilt가 클수록 면이 많이 기울어진 것처럼 대비가 커집니다.
        /// </summary>
        public void FacetFan(IList<Vector2> points, Vector2 apex, Color dark, Color mid, Color light, float tilt = 0.6f, Vector2? lightFrom = null)
        {
            Vector2 lf = lightFrom ?? LightFrom;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                Vector2 mid2 = (a + b) * 0.5f;
                Vector2 n = (mid2 - apex);
                n = n.sqrMagnitude > 0.0001f ? n.normalized : Vector2.zero;
                float shade = Vector2.Dot(n, lf) * tilt;
                Tri(Shade(dark, mid, light, shade), apex, a, b);
            }
        }

        /// <summary>-1(어두움) ~ 1(밝음) 값을 세 색 사이로 보간합니다.</summary>
        public static Color Shade(Color dark, Color mid, Color light, float shade)
        {
            shade = Mathf.Clamp(shade, -1f, 1f);
            return shade >= 0f ? Color.Lerp(mid, light, shade) : Color.Lerp(mid, dark, -shade);
        }

        /// <summary>모서리를 45도로 깎은 사각형(팔각형) 꼭짓점을 시계 방향으로 반환합니다.</summary>
        public static Vector2[] Chamfer(float x, float y, float w, float h, float cut)
        {
            cut = Mathf.Min(cut, Mathf.Min(w, h) * 0.5f);
            return new[]
            {
                new Vector2(x + cut, y), new Vector2(x + w - cut, y),
                new Vector2(x + w, y + cut), new Vector2(x + w, y + h - cut),
                new Vector2(x + w - cut, y + h), new Vector2(x + cut, y + h),
                new Vector2(x, y + h - cut), new Vector2(x, y + cut),
            };
        }

        /// <summary>Chamfer를 inset만큼 안쪽으로 줄인 팔각형을 반환합니다(45도 컷 유지).</summary>
        public static Vector2[] ChamferInset(float x, float y, float w, float h, float cut, float inset)
        {
            return Chamfer(x + inset, y + inset, w - inset * 2f, h - inset * 2f, Mathf.Max(0f, cut - inset * 0.4142f));
        }

        /// <summary>
        /// 같은 꼭짓점 수의 바깥/안쪽 다각형 사이 테두리를 면 단위로 칠합니다.
        /// raised=true면 빛을 향한 면이 밝고, false면 오목하게(반대로) 칠합니다.
        /// </summary>
        public void Rim(Vector2[] outer, Vector2[] inner, Color dark, Color mid, Color light, bool raised = true)
        {
            for (int i = 0; i < outer.Length; i++)
            {
                int j = (i + 1) % outer.Length;
                Vector2 edge = outer[j] - outer[i];
                Vector2 normal = new Vector2(edge.y, -edge.x).normalized; // 시계 방향 다각형의 바깥 법선(y 아래 좌표계)
                float shade = Vector2.Dot(normal, LightFrom) * (raised ? 1f : -1f);
                Poly(Shade(dark, mid, light, shade), outer[i], outer[j], inner[j], inner[i]);
            }
        }

        /// <summary>정다각형 꼭짓점을 반환합니다. startDeg=-90이면 위쪽 꼭짓점부터 시작합니다.</summary>
        public static Vector2[] Regular(Vector2 center, float radius, int sides, float startDeg = -90f, float scaleY = 1f)
        {
            Vector2[] pts = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = (startDeg + 360f * i / sides) * Mathf.Deg2Rad;
                pts[i] = center + new Vector2(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius * scaleY);
            }
            return pts;
        }

        /// <summary>다각형을 꼭짓점 법선 방향으로 밀어 외곽선용 다각형을 만듭니다.</summary>
        public static Vector2[] Offset(IList<Vector2> pts, float distance)
        {
            int n = pts.Count;
            Vector2[] result = new Vector2[n];
            float area = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 a = pts[i], b = pts[(i + 1) % n];
                area += a.x * b.y - b.x * a.y;
            }
            float sign = area >= 0f ? 1f : -1f; // y 아래 좌표계에서 시계 방향이면 area > 0
            for (int i = 0; i < n; i++)
            {
                Vector2 prev = pts[(i - 1 + n) % n], cur = pts[i], next = pts[(i + 1) % n];
                Vector2 e1 = (cur - prev).normalized, e2 = (next - cur).normalized;
                Vector2 n1 = new Vector2(e1.y, -e1.x) * sign, n2 = new Vector2(e2.y, -e2.x) * sign;
                Vector2 nn = (n1 + n2);
                float len = nn.magnitude;
                if (len < 0.0001f) { result[i] = cur + n1 * distance; continue; }
                nn /= len;
                float miter = distance / Mathf.Max(0.35f, Vector2.Dot(nn, n1));
                result[i] = cur + nn * miter;
            }
            return result;
        }

        public static Vector2 Centroid(IList<Vector2> pts)
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2 p in pts) sum += p;
            return sum / pts.Count;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out Color c);
            return c;
        }

        /// <summary>슈퍼샘플 버퍼를 최종 해상도로 줄여 Texture2D로 만듭니다(Unity 텍스처는 아래쪽이 원점).</summary>
        public Texture2D ToTexture()
        {
            Texture2D tex = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[_width * _height];
            int w = _width * _ss;
            float norm = 1f / (_ss * _ss);
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Color sum = Color.clear;
                    for (int sy = 0; sy < _ss; sy++)
                    {
                        int row = (y * _ss + sy) * w + x * _ss;
                        for (int sx = 0; sx < _ss; sx++) sum += _buffer[row + sx];
                    }
                    sum *= norm;
                    if (sum.a > 0.0001f) sum = new Color(sum.r / sum.a, sum.g / sum.a, sum.b / sum.a, sum.a);
                    pixels[(_height - 1 - y) * _width + x] = sum;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
