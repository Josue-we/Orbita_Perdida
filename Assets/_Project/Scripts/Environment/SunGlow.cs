using UnityEngine;

namespace Lumen.Environment
{
    /// <summary>
    /// Brilho (corona) ao redor do Sol: dois sprites com gradiente radial, sempre virados
    /// para a camera. E bem mais barato que os particle systems "Glow" do pacote (so 2 quads),
    /// entao nao pesa em placa de video integrada. Fica atras do globo do Sol, entao o disco
    /// continua nitido e so o halo aparece em volta.
    /// </summary>
    public class SunGlow : MonoBehaviour
    {
        private Transform _inner, _outer, _cam;

        private void Awake()
        {
            float radius = MeasureRadius();
            _outer = CreateLayer("SunGlowOuter", radius * 7f, new Color(1f, 0.62f, 0.18f, 0.50f), 3.2f, 0);
            _inner = CreateLayer("SunGlowInner", radius * 3.2f, new Color(1f, 0.90f, 0.55f, 0.85f), 2.4f, 1);
        }

        private float MeasureRadius()
        {
            float r = 1f;
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                if (rend is ParticleSystemRenderer) continue;
                r = Mathf.Max(r, rend.bounds.extents.magnitude * 0.58f);
            }
            return r;
        }

        private Transform CreateLayer(string layerName, float diameter, Color tint, float falloff, int order)
        {
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float a = Mathf.Pow(1f - d, falloff);
                    px[y * size + x] = new Color(tint.r, tint.g, tint.b, tint.a * a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            var go = new GameObject(layerName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sr.sortingOrder = order;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;

            float parentScale = Mathf.Max(transform.lossyScale.x, 0.0001f);
            go.transform.localScale = Vector3.one * (diameter / parentScale);
            return go.transform;
        }

        private void LateUpdate()
        {
            if (_cam == null)
            {
                var main = Camera.main;
                if (main == null) return;
                _cam = main.transform;
            }
            Quaternion face = Quaternion.LookRotation(_cam.forward, _cam.up);
            if (_outer != null) _outer.rotation = face;
            if (_inner != null) _inner.rotation = face;
        }
    }
}
