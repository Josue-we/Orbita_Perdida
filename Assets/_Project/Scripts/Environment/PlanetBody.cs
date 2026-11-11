using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Environment
{
    /// <summary>
    /// Corpo solido de um planeta (ou do Sol): a nave nao entra nele. Como o LUMEN nao usa
    /// fisica de Rigidbody, a colisao e uma esfera simples checada pelo LumenController
    /// (PushOut), o que e barato e funciona em qualquer maquina. O raio e medido da malha do
    /// proprio planeta (sem aneis nem brilhos), entao acompanha a escala da cena.
    /// </summary>
    public class PlanetBody : MonoBehaviour
    {
        private static readonly List<PlanetBody> All = new List<PlanetBody>();

        /// <summary>Folga em volta da superficie para a nave "parar" um pouco antes de encostar.</summary>
        public const float SurfaceMargin = 8f;

        public float Radius { get; private set; }

        private void Awake()
        {
            Radius = Measure();
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }

        /// <summary>Raio do globo: maior extensao de malha que nao seja anel, brilho ou LOD.</summary>
        private float Measure()
        {
            float r = 0f;
            foreach (var mr in GetComponentsInChildren<MeshRenderer>(true))
            {
                string n = mr.name;
                if (n.Contains("Ring") || n.Contains("Glow") || n.Contains("Lod") || n.Contains("FX")) continue;
                r = Mathf.Max(r, mr.bounds.extents.x);
            }
            // Sem malha reconhecivel: esfera de diametro igual a escala (padrao dos prefabs do pacote).
            if (r < 0.01f) r = transform.lossyScale.x * 0.5f;
            return r;
        }

        /// <summary>
        /// Empurra a posicao para fora de qualquer planeta. Devolve a nova posicao; hit indica
        /// se encostou e normal aponta do planeta para a nave (para tirar a velocidade que entra).
        /// </summary>
        public static Vector3 PushOut(Vector3 position, out bool hit, out Vector3 normal)
        {
            hit = false;
            normal = Vector3.up;
            for (int i = 0; i < All.Count; i++)
            {
                PlanetBody body = All[i];
                Vector3 offset = position - body.transform.position;
                float min = body.Radius + SurfaceMargin;
                float sqr = offset.sqrMagnitude;
                if (sqr >= min * min) continue;

                // Exatamente no centro (teletransporte): sai para cima.
                normal = sqr < 0.0001f ? Vector3.up : offset.normalized;
                position = body.transform.position + normal * min;
                hit = true;
            }
            return position;
        }
    }
}
