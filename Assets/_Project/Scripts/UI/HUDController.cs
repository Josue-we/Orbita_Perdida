using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;

namespace Lumen.UI
{
    /// <summary>
    /// HUD minimo para o Tutorial: barra de energia + texto de instrucao.
    /// Paineis de quiz e HUD de minigame entram junto com a Fase 2.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Slider energyBar;
        [SerializeField] private Text objectiveLabel;

        private void OnEnable()
        {
            EventBus.OnEnergyChanged += HandleEnergyChanged;
        }

        private void OnDisable()
        {
            EventBus.OnEnergyChanged -= HandleEnergyChanged;
        }

        private void Start()
        {
            if (objectiveLabel != null)
                objectiveLabel.text = "Use WASD para mover. Space / Ctrl para subir e descer.";
        }

        private void HandleEnergyChanged(float current, float max)
        {
            if (energyBar == null) return;
            energyBar.maxValue = max;
            energyBar.value = current;
        }

        /// <summary>Usado pelo TutorialSceneBuilder para ligar as referencias de UI sem reflection.</summary>
        public void Configure(Slider bar, Text label)
        {
            energyBar = bar;
            objectiveLabel = label;
        }
    }
}
