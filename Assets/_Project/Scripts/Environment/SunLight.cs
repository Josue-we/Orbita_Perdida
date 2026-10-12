using UnityEngine;
using UnityEngine.Rendering;
using Lumen.Gameplay;

namespace Lumen.Environment
{
    /// <summary>
    /// Fica na Directional Light: a luz sempre VEM DO SOL. A cada quadro a direcao e
    /// recalculada do Sol para a nave, entao a face iluminada dos planetas e da nave
    /// muda conforme a viagem. Sombras ficam DESLIGADAS (custo alto em placa de video
    /// integrada, e nada no jogo precisa delas).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class SunLight : MonoBehaviour
    {
        [SerializeField] private Transform sun;

        private Light _light;
        private Transform _ship;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Directional;
            _light.shadows = LightShadows.None;
            _light.color = new Color(1f, 0.96f, 0.88f);
            _light.intensity = 1.25f;

            // Luz ambiente fraca e fixa: o lado escuro da nave/planetas continua visivel.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.20f, 0.23f, 0.31f);
        }

        private void LateUpdate()
        {
            if (sun == null)
            {
                var found = GameObject.Find("Sun");
                if (found != null) sun = found.transform;
                else return;
            }

            if (_ship == null)
            {
                var controller = Object.FindFirstObjectByType<LumenController>();
                if (controller != null) _ship = controller.transform;
                else return;
            }

            Vector3 dir = _ship.position - sun.position;
            if (dir.sqrMagnitude < 1f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized);
        }

        public void SetSun(Transform sunTransform) => sun = sunTransform;
    }
}
