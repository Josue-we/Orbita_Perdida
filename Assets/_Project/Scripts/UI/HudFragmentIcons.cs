using UnityEngine;
using UnityEngine.UI;

namespace Lumen.UI
{
    /// <summary>
    /// Fileira de icones de cristal sob o contador de fragmentos: apagados enquanto o fragmento
    /// do planeta nao foi coletado, dourados (com um "pulinho") depois. Construida por codigo,
    /// entao funciona em cena ja montada.
    /// </summary>
    public class HudFragmentIcons : MonoBehaviour
    {
        private static readonly Color Dim = new Color(0.45f, 0.55f, 0.72f, 0.45f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.28f, 1f);
        private const float PopSeconds = 0.45f;

        private Image[] _icons;
        private float[] _pop;
        private int _collected;
        private CanvasGroup _group;

        public static HudFragmentIcons Create(Transform canvas, int total)
        {
            var go = new GameObject("FragmentIcons", typeof(RectTransform));
            go.transform.SetParent(canvas, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.60f, 0.855f);
            rt.anchorMax = new Vector2(0.98f, 0.915f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.spacing = 12f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var icons = go.AddComponent<HudFragmentIcons>();
            icons._group = go.AddComponent<CanvasGroup>();
            icons._group.blocksRaycasts = false;
            icons._group.interactable = false;
            icons.Build(total);
            return icons;
        }

        private void Build(int total)
        {
            Sprite crystal = CreateCrystalSprite();
            _icons = new Image[total];
            _pop = new float[total];

            for (int i = 0; i < total; i++)
            {
                var icon = new GameObject("Fragment" + (i + 1), typeof(RectTransform));
                icon.transform.SetParent(transform, false);
                ((RectTransform)icon.transform).sizeDelta = new Vector2(46f, 56f);

                var img = icon.AddComponent<Image>();
                img.sprite = crystal;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = Dim;
                _icons[i] = img;
            }
        }

        /// <summary>Acende o proximo icone.</summary>
        public void Collect()
        {
            if (_icons == null || _collected >= _icons.Length) return;
            _icons[_collected].color = Gold;
            _pop[_collected] = PopSeconds;
            _collected++;
        }

        private void Update()
        {
            if (_icons == null) return;

            // Na tela final o resumo ja mostra os fragmentos: esconde os icones.
            if (_group != null)
            {
                var gm = Lumen.Core.GameManager.Instance;
                bool hide = gm != null && gm.CurrentState == Lumen.Core.GameState.Ended;
                _group.alpha = Mathf.MoveTowards(_group.alpha, hide ? 0f : 1f, Time.unscaledDeltaTime * 4f);
            }

            for (int i = 0; i < _icons.Length; i++)
            {
                if (_pop[i] <= 0f) continue;
                _pop[i] = Mathf.Max(0f, _pop[i] - Time.unscaledDeltaTime);
                float u = 1f - _pop[i] / PopSeconds;
                float scale = 1f + 0.55f * Mathf.Sin(u * Mathf.PI);
                _icons[i].rectTransform.localScale = Vector3.one * scale;
            }
        }

        /// <summary>Cristal (losango alongado) com brilho no centro, desenhado por codigo.</summary>
        private static Sprite CreateCrystalSprite()
        {
            const int w = 64, h = 80;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var px = new Color32[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx) / (w * 0.46f);
                    float dy = Mathf.Abs(y + 0.5f - cy) / (h * 0.47f);
                    float d = dx + dy;                       // <= 1 dentro do losango
                    float alpha = Mathf.Clamp01((1f - d) * 18f);
                    float shade = 0.72f + 0.28f * Mathf.Clamp01(1f - d);   // mais claro no centro
                    byte v = (byte)(255f * shade);
                    px[y * w + x] = new Color32(v, v, v, (byte)(255f * alpha));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
