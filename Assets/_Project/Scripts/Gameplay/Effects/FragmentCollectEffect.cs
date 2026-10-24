using System;
using System.Collections;
using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay.Effects
{
    /// <summary>
    /// Coleta do fragmento de conhecimento: um objeto brilhante se materializa perto do
    /// satelite, e e atraido ate ele. Na chegada: explosao de particulas douradas e um
    /// brilho que envolve o LUMEN. Tudo feito com sprites leves (ver SpriteFx).
    /// </summary>
    public static class FragmentCollectEffect
    {
        private static readonly Color Gold = new Color(1f, 0.82f, 0.28f, 1f);

        /// <summary>
        /// Corrotina: materializa o fragmento em <paramref name="start"/>, atrai ate a nave e
        /// chama <paramref name="onArrived"/> na chegada (aqui entram o som, o contador e a energia).
        /// </summary>
        public static IEnumerator FlyTo(Transform ship, Vector3 start, Action onArrived)
        {
            var root = new GameObject("KnowledgeFragment");
            root.transform.position = start;
            var halo = SpriteFx.Make("Halo", root.transform, new Color(1f, 0.74f, 0.18f, 0.7f), 9f, 4);
            var core = SpriteFx.Make("Core", root.transform, new Color(1f, 0.97f, 0.82f, 1f), 3.2f, 5);

            // 1) Materializa: cresce do nada ate o tamanho final.
            float t = 0f;
            while (t < GameTuning.FragmentMaterializeSeconds)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / GameTuning.FragmentMaterializeSeconds);
                root.transform.localScale = Vector3.one * k;
                SpriteFx.Billboard(halo.transform);
                SpriteFx.Billboard(core.transform);
                yield return null;
            }

            // 2) Voa ate o satelite, acelerando, com uma leve curva lateral.
            Vector3 toShip = ship.position - start;
            Vector3 side = Vector3.Cross(Vector3.up, toShip);
            side = side.sqrMagnitude > 0.001f ? side.normalized : Vector3.right;

            t = 0f;
            while (t < GameTuning.FragmentFlySeconds)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / GameTuning.FragmentFlySeconds);
                float eased = u * u;
                Vector3 target = ship.position + Vector3.up * 0.5f;
                root.transform.position = Vector3.Lerp(start, target, eased) + side * (Mathf.Sin(u * Mathf.PI) * 3f * (1f - u));

                float pulse = 1f + 0.18f * Mathf.Sin(Time.time * 24f);
                root.transform.localScale = Vector3.one * pulse * Mathf.Lerp(1f, 0.6f, u);
                SpriteFx.Billboard(halo.transform);
                SpriteFx.Billboard(core.transform);
                yield return null;
            }

            // 3) Chegou: efeitos + evento.
            Vector3 hit = ship.position + Vector3.up * 0.5f;
            UnityEngine.Object.Destroy(root);
            GoldBurst.Spawn(hit);
            ShipGlowPulse.Spawn(ship);
            onArrived?.Invoke();
        }

        /// <summary>Explosao de particulas douradas (sprites que se afastam e somem).</summary>
        private class GoldBurst : MonoBehaviour
        {
            private const int Count = 26;
            private const float Life = 1.1f;

            private SpriteRenderer[] _sprites;
            private Vector3[] _velocity;
            private float[] _size;
            private float _age;

            public static void Spawn(Vector3 position)
            {
                var go = new GameObject("GoldBurst");
                go.transform.position = position;
                go.AddComponent<GoldBurst>();
            }

            private void Awake()
            {
                _sprites = new SpriteRenderer[Count];
                _velocity = new Vector3[Count];
                _size = new float[Count];
                for (int i = 0; i < Count; i++)
                {
                    _size[i] = UnityEngine.Random.Range(0.6f, 1.4f);
                    _sprites[i] = SpriteFx.Make("p", transform, Gold, _size[i], 6);
                    _velocity[i] = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(8f, 22f);
                }
            }

            private void Update()
            {
                _age += Time.deltaTime;
                float u = Mathf.Clamp01(_age / Life);
                float drag = Mathf.Exp(-2.5f * _age);

                for (int i = 0; i < Count; i++)
                {
                    var t = _sprites[i].transform;
                    t.position += _velocity[i] * drag * Time.deltaTime;
                    SpriteFx.Billboard(t);
                    SpriteFx.SetWorldDiameter(t, _size[i] * (1f - 0.6f * u));
                    var c = Gold;
                    c.a = 1f - u;
                    _sprites[i].color = c;
                }

                if (_age >= Life) Destroy(gameObject);
            }
        }

        /// <summary>Brilho dourado que "respira" em volta do LUMEN ao receber o fragmento.</summary>
        private class ShipGlowPulse : MonoBehaviour
        {
            private const float Life = 1.2f;
            private SpriteRenderer _glow;
            private float _age;

            public static void Spawn(Transform ship)
            {
                var go = new GameObject("ShipGlowPulse");
                go.transform.SetParent(ship, false);
                go.AddComponent<ShipGlowPulse>();
            }

            private void Awake()
            {
                _glow = SpriteFx.Make("Glow", transform, new Color(1f, 0.85f, 0.35f, 0.8f), 6f, 3);
            }

            private void Update()
            {
                _age += Time.deltaTime;
                float u = Mathf.Clamp01(_age / Life);
                SpriteFx.Billboard(_glow.transform);
                SpriteFx.SetWorldDiameter(_glow.transform, Mathf.Lerp(6f, 15f, Mathf.Sqrt(u)));
                var c = new Color(1f, 0.85f, 0.35f, 0.8f * (1f - u));
                _glow.color = c;
                if (_age >= Life) Destroy(gameObject);
            }
        }
    }
}
