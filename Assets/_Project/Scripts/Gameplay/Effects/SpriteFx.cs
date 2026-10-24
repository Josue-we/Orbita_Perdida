using UnityEngine;

namespace Lumen.Gameplay.Effects
{
    /// <summary>
    /// Pecas de efeito visual leves: sprites com gradiente radial feitos por codigo e sempre
    /// virados para a camera. Sem ParticleSystem e sem luzes: barato em placa de video integrada.
    /// </summary>
    public static class SpriteFx
    {
        private static Sprite _soft;

        /// <summary>Bolinha brilhante: forte no centro, some suavemente nas bordas.</summary>
        public static Sprite Soft
        {
            get
            {
                if (_soft != null) return _soft;

                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                        float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                        float a = Mathf.Pow(1f - d, 2.2f);
                        px[y * size + x] = new Color(1f, 1f, 1f, a);
                    }
                }
                tex.SetPixels32(px);
                tex.Apply(false, true);
                _soft = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
                return _soft;
            }
        }

        /// <summary>Cria um sprite filho com o diametro pedido (em unidades do mundo).</summary>
        public static SpriteRenderer Make(string name, Transform parent, Color color, float worldDiameter, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Soft;
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
            SetWorldDiameter(go.transform, worldDiameter);
            return sr;
        }

        /// <summary>Ajusta o tamanho em unidades do mundo, mesmo com o pai escalado.</summary>
        public static void SetWorldDiameter(Transform t, float worldDiameter)
        {
            float parentScale = t.parent != null ? Mathf.Max(t.parent.lossyScale.x, 0.0001f) : 1f;
            t.localScale = Vector3.one * (worldDiameter / parentScale);
        }

        public static void Billboard(Transform t)
        {
            var cam = Camera.main;
            if (cam != null) t.rotation = cam.transform.rotation;
        }
    }
}
