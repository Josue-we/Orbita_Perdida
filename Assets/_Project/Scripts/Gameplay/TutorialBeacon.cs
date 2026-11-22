using UnityEngine;
using Lumen.Gameplay.Effects;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Farol de saida do tutorial: um ponto de luz brilhante e pulsante, feito so com sprites
    /// (sem ParticleSystem e sem luz), entao e leve em placa de video integrada. Fica sempre
    /// visivel de longe e some quando o jogador o atravessa.
    /// </summary>
    public class TutorialBeacon : MonoBehaviour
    {
        private const float HaloDiameter = 150f;
        private const float MidDiameter = 60f;
        private const float CoreDiameter = 20f;

        private Transform _visual, _halo, _mid;
        private float _t;

        private void Awake()
        {
            // Remove o farol antigo (esfera do pacote), que dependia de efeitos removidos por desempenho.
            var old = transform.Find("TutorialBeacon");
            if (old != null) Destroy(old.gameObject);

            _visual = new GameObject("FarolVisual").transform;
            _visual.SetParent(transform, false);

            _halo = SpriteFx.Make("Halo", _visual, new Color(0.25f, 0.85f, 1f, 0.55f), HaloDiameter, 0).transform;
            _mid = SpriteFx.Make("Meio", _visual, new Color(0.6f, 0.95f, 1f, 0.85f), MidDiameter, 1).transform;
            SpriteFx.Make("Nucleo", _visual, new Color(1f, 1f, 1f, 1f), CoreDiameter, 2);
        }

        private void LateUpdate()
        {
            if (_visual == null) return;

            _t += Time.deltaTime;
            float pulse = 1f + 0.12f * Mathf.Sin(_t * 2.6f);
            SpriteFx.SetWorldDiameter(_halo, HaloDiameter * pulse);
            SpriteFx.SetWorldDiameter(_mid, MidDiameter * (2f - pulse));

            var cam = Camera.main;
            if (cam != null) _visual.rotation = cam.transform.rotation;
        }

        public void Hide()
        {
            if (_visual != null) Destroy(_visual.gameObject);
            _visual = null;
        }
    }
}
