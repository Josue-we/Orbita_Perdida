using UnityEngine;
using Lumen.Core;

namespace Lumen.Cameras
{
    /// <summary>
    /// Camera de perseguicao com ORIENTACAO FIXA no mundo (sem Cinemachine).
    ///
    /// O detalhe que importa: a camera nao gira junto com a nave. A versao
    /// anterior usava target.TransformDirection(offset), ou seja, o offset era
    /// a "tras da nave" - entao qualquer giro da nave girava a tela inteira e o
    /// mundo parecia voltar para o centro sozinho a cada manobra lateral.
    ///
    /// Agora a camera fica num deslocamento fixo do mundo, olhando a nave de cima
    /// e de tras. Resultado: o horizonte nunca gira, W sobe na tela, D vai para a
    /// direita, e a nave pode apontar para onde quiser sem mexer na visao.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 32f, -28f);
        [SerializeField] private float positionSmoothTime = 0.16f;

        private Vector3 _velocity;
        private Quaternion _framing = Quaternion.identity;

        private void Awake()
        {
            // Tuning oficial sempre vence o valor salvo no Inspector (ver GameTuning).
            offset = GameTuning.CameraOffset;
            positionSmoothTime = GameTuning.CameraPositionSmooth;
            _framing = BuildFraming();
            transform.rotation = _framing;
        }

        private void Start()
        {
            // Primeira tela sem o "vovo" do SmoothDamp atravessando o mapa.
            SnapToTarget();
        }

        /// <summary>Rotacao fixa: o angulo de cima combinando com o offset.</summary>
        private Quaternion BuildFraming()
        {
            float horizontal = Mathf.Max(0.001f, new Vector2(offset.x, offset.z).magnitude);
            float pitch = Mathf.Atan2(offset.y, horizontal) * Mathf.Rad2Deg;
            return Quaternion.Euler(pitch, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);
            transform.rotation = _framing;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>
        /// Cola a camera no alvo na hora. Usado no resgate: sem isso o SmoothDamp
        /// atravessa o mapa inteiro em camera lenta quando a nave e teletransportada.
        /// </summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            transform.position = target.position + offset;
            transform.rotation = _framing == Quaternion.identity ? BuildFraming() : _framing;
            _velocity = Vector3.zero;
        }
    }
}
