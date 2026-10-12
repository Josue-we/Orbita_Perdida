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
        // Valores oficiais (o construtor de cena tambem usa): luz forte e ambiente fraco
        // deixam a iluminacao bem direcional - um lado claro e o outro escuro.
        public const float Intensity = 1.5f;
        public static readonly Color LightColor = new Color(1f, 0.95f, 0.85f);
        public static readonly Color AmbientColor = new Color(0.13f, 0.15f, 0.22f);

        [SerializeField] private Transform sun;

        private Light _light;
        private Transform _ship;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _light.type = LightType.Directional;
            _light.shadows = LightShadows.None;
            _light.color = LightColor;
            _light.intensity = Intensity;

            // Luz ambiente fraca e fixa: o lado escuro da nave/planetas continua visivel.
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
        }

        private void LateUpdate()
        {
            if (sun == null)
            {
                var found = GameObject.Find("Sun");
                if (found != null) sun = found.transform;
                else return;
            }

            if (sun.GetComponent<SunGlow>() == null) sun.gameObject.AddComponent<SunGlow>();

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
