using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Zona que marca o fim do Tutorial quando o LUMEN a atravessa.
    /// Dois caminhos independentes garantem que ele SEMPRE conclui:
    /// 1) OnTriggerEnter via fisica; 2) checagem por distancia a cada 0,2s
    /// (nao depende de tag nem de colisao). Expande os limites de voo e
    /// destroi o farol visivel como sinal claro de conclusao.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialExitTrigger : MonoBehaviour
    {
        [SerializeField] private float expandedBoundsRadius = 220f;

        private bool _triggered;
        private Transform _player;
        private float _checkTimer;

        private float WorldRadius => transform.lossyScale.x * 0.5f;

        private void Update()
        {
            if (_triggered) return;

            if (_player == null)
            {
                _player = FindPlayer();
                if (_player == null) return;
            }

            _checkTimer -= Time.deltaTime;
            if (_checkTimer > 0f) return;
            _checkTimer = 0.2f;

            // Fallback sem fisica: jogador dentro da zona do farol => tutorial conclui.
            if (Vector3.Distance(transform.position, _player.position) <= WorldRadius + 2f)
                CompleteTutorial();
        }

        private static bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            return other.GetComponentInParent<LumenController>() != null;
        }

        private static Transform FindPlayer()
        {
            var tagged = GameObject.FindWithTag("Player");
            if (tagged != null) return tagged.transform;

            var controller = Object.FindFirstObjectByType<LumenController>();
            return controller == null ? null : controller.transform;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!IsPlayer(other)) return;
            CompleteTutorial();
        }

        private void CompleteTutorial()
        {
            if (_triggered) return;
            _triggered = true;

            if (_player == null) _player = FindPlayer();
            var lumen = _player != null ? _player.GetComponent<LumenController>() : null;
            if (lumen != null) lumen.ExpandBounds(expandedBoundsRadius);

            // O farol de saida some quando o jogador atravessa: sinal visual claro
            // de que o tutorial terminou e o jogo comecou.
            var beacon = transform.Find("TutorialBeacon");
            if (beacon != null) Destroy(beacon.gameObject);

            EventBus.RaiseTutorialCompleted();
            Debug.Log("[LUMEN] Tutorial concluido - o jogo comecou. Proxima parada: Netuno.");
        }
    }
}