using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Zona invisivel que marca o fim do Tutorial quando o LUMEN a atravessa.
    /// Na proxima iteracao (Fase 2 - Netuno), isso sera substituido pela transicao
    /// real de fase via PhaseManager; por enquanto so dispara o evento e loga.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TutorialExitTrigger : MonoBehaviour
    {
        private bool _triggered;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered) return;
            if (!other.CompareTag("Player")) return;

            _triggered = true;
            EventBus.RaiseTutorialCompleted();
            Debug.Log("[LUMEN] Tutorial concluido - proxima parada: Netuno.");
        }
    }
}
