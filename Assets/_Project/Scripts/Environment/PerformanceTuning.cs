using UnityEngine;
using Lumen.Gameplay;

namespace Lumen.Environment
{
    /// <summary>
    /// Ajustes de desempenho aplicados ao carregar a cena, pensados para maquinas SEM
    /// placa de video dedicada:
    ///  - remove os sistemas de particulas "Glow" dos planetas e do Sol. Cada planeta do
    ///    pacote traz um billboard de brilho enorme (no Saturno, ~1000 unidades) que cobre
    ///    a tela inteira com transparencia: custo de preenchimento altissimo;
    ///  - desliga sombras de luzes direcionais;
    ///  - limita o jogo a 60 quadros por segundo (poupa CPU/GPU e bateria).
    /// Valem em qualquer cena ja montada (nao dependem de remontar nada).
    /// </summary>
    public static class PerformanceTuning
    {
        private static readonly string[] HeavyEffectNames = { "Glow", "Sun Glow", "Sun FX", "Particle System" };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Apply()
        {
            Application.targetFrameRate = 60;

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (light.type == LightType.Directional) light.shadows = LightShadows.None;

            RemoveHeavyEffects();
        }

        public static void RemoveHeavyEffects()
        {
            var systems = Object.FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ps in systems)
            {
                if (ps == null) continue;
                var tr = ps.transform;
                if (tr.parent == null) continue;                                   // so efeitos filhos de prefab
                if (tr.root.GetComponent<LumenController>() != null) continue;    // nunca mexe na nave
                if (System.Array.IndexOf(HeavyEffectNames, ps.gameObject.name) < 0) continue;

                Object.Destroy(ps.gameObject);
            }
        }
    }
}
