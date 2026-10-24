using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>
    /// Legenda de controles no canto esquerdo da tela, com teclas desenhadas como "teclas de
    /// teclado". Fica visivel o jogo todo (antes os controles so apareciam no tutorial).
    /// </summary>
    public class HudControlsLegend : MonoBehaviour
    {
        private const int KeyFontSize = 24;
        private const int LabelFontSize = 26;
        private const float KeyHeight = 40f;

        private CanvasGroup _group;

        private void Update()
        {
            // Some durante o quiz (para nao invadir o painel) e na tela final.
            if (_group == null) return;
            var gm = Lumen.Core.GameManager.Instance;
            bool hide = gm != null && (gm.CurrentState == Lumen.Core.GameState.Challenge
                                       || gm.CurrentState == Lumen.Core.GameState.Ended);
            _group.alpha = Mathf.MoveTowards(_group.alpha, hide ? 0f : 1f, Time.unscaledDeltaTime * 4f);
        }

        public static HudControlsLegend Create(Transform canvas)
        {
            var go = new GameObject("ControlsLegend", typeof(RectTransform));
            go.transform.SetParent(canvas, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.50f);
            rt.anchorMax = new Vector2(0.30f, 0.735f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var column = go.AddComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperLeft;
            column.spacing = 8f;
            column.childControlWidth = false;
            column.childControlHeight = false;
            column.childForceExpandWidth = false;
            column.childForceExpandHeight = false;

            var legend = go.AddComponent<HudControlsLegend>();
            legend._group = go.AddComponent<CanvasGroup>();
            legend._group.blocksRaycasts = false;
            legend._group.interactable = false;
            Sprite keySprite = CreateKeySprite();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            legend.AddRow(go.transform, keySprite, font, new[] { "W", "S" }, "avançar / recuar");
            legend.AddRow(go.transform, keySprite, font, new[] { "A", "D" }, "girar");
            legend.AddRow(go.transform, keySprite, font, new[] { "Espaço", "Ctrl" }, "subir / descer");
            legend.AddRow(go.transform, keySprite, font, new[] { "X" }, "frear");
            legend.AddRow(go.transform, keySprite, font, new[] { "Shift" }, "boost");
            return legend;
        }

        private void AddRow(Transform parent, Sprite keySprite, Font font, string[] keys, string label)
        {
            var row = new GameObject("Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            ((RectTransform)row.transform).sizeDelta = new Vector2(560f, KeyHeight);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.spacing = 8f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            foreach (string key in keys)
            {
                var cap = new GameObject("Key_" + key, typeof(RectTransform));
                cap.transform.SetParent(row.transform, false);
                float width = key.Length <= 1 ? KeyHeight : 22f + key.Length * 15f;
                ((RectTransform)cap.transform).sizeDelta = new Vector2(width, KeyHeight);

                var img = cap.AddComponent<Image>();
                img.sprite = keySprite;
                img.type = Image.Type.Sliced;
                img.raycastTarget = false;
                img.color = new Color(0.10f, 0.16f, 0.28f, 0.78f);

                var letter = NewText(cap.transform, font, key, KeyFontSize, TextAnchor.MiddleCenter,
                    new Color(0.80f, 0.93f, 1f, 1f));
                Stretch(letter.rectTransform);
            }

            var text = NewText(row.transform, font, label, LabelFontSize, TextAnchor.MiddleLeft,
                new Color(0.86f, 0.93f, 1f, 0.88f));
            text.rectTransform.sizeDelta = new Vector2(380f, KeyHeight);
        }

        private static Text NewText(Transform parent, Font font, string value, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Retangulo de cantos arredondados com borda clara (9-slice), feito por codigo.</summary>
        private static Sprite CreateKeySprite()
        {
            const int size = 32;
            const float radius = 8f, border = 2f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - half) - (half - radius);
                    float qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                    float dist = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f))
                                 + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;        // <0 dentro
                    float inside = Mathf.Clamp01(0.5f - dist);
                    float edge = Mathf.Clamp01(1f - Mathf.Abs(dist + border * 0.5f) / (border * 0.5f + 0.5f));
                    float shade = Mathf.Lerp(0.55f, 1f, edge);
                    byte v = (byte)(255f * shade);
                    px[y * size + x] = new Color32(v, v, v, (byte)(255f * inside));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
                0, SpriteMeshType.FullRect, new Vector4(10f, 10f, 10f, 10f));
        }
    }
}
