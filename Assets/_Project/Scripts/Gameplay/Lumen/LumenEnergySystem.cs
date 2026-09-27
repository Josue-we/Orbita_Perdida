using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Controla a energia/combustivel do LUMEN. O consumo acontece so com os
    /// propulsores acesos (um foguete parado nao queima combustivel), e zera
    /// significa parar: o LumenController trava a nave e aciona o resgate.
    /// O Refill() e chamado pelo sistema de desafios ao coletar um fragmento
    /// de conhecimento e pelo proprio resgate quando o tanque acaba.
    /// </summary>
    public class LumenEnergySystem : MonoBehaviour
    {
        [SerializeField] private float maxEnergy = GameTuning.MaxEnergy;

        [Header("Consumo por segundo")]
        [Tooltip("Com os propulsores acesos.")]
        [SerializeField] private float thrustDrainPerSecond = GameTuning.ThrustDrainPerSecond;

        [Tooltip("Parado, sem propulsores. 0 = não queima combustível só por existir.")]
        [SerializeField] private float idleDrainPerSecond = GameTuning.IdleDrainPerSecond;

        public float CurrentEnergy { get; private set; }
        public float MaxEnergy => maxEnergy;
        public bool IsEmpty => CurrentEnergy <= 0f;

        private void Start()
        {
            // O tuning oficial sempre vence o valor salvo no Inspector: e o que
            // garante que o ajuste chegue mesmo em cena montada antes dele.
            Configure(GameTuning.MaxEnergy, GameTuning.ThrustDrainPerSecond, GameTuning.IdleDrainPerSecond);

            CurrentEnergy = maxEnergy;
            EventBus.RaiseEnergyChanged(CurrentEnergy, maxEnergy);
        }

        public void Tick(float deltaTime, bool isThrusting)
        {
            // Tanque vazio nao consome mais nada (e nem precisa repetir o evento):
            // quem assume o resgate a partir daqui e o LumenController.
            if (IsEmpty) return;

            float drain = isThrusting ? thrustDrainPerSecond : idleDrainPerSecond;
            if (drain <= 0f) return;

            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - drain * deltaTime);
            EventBus.RaiseEnergyChanged(CurrentEnergy, maxEnergy);
        }

        public void Refill(float amount)
        {
            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
            EventBus.RaiseEnergyChanged(CurrentEnergy, maxEnergy);
        }

        /// <summary>Enche o tanque - usado pelo resgate quando o combustivel acaba.</summary>
        public void RefillToFull()
        {
            Refill(maxEnergy);
        }

        /// <summary>
        /// Aplica os numeros de consumo. O construtor de cena chama isso sempre,
        /// inclusive em cena ja montada: [SerializeField] guarda o valor antigo
        /// dentro do componente, entao so trocar o default do script nao mudaria
        /// nada no que o jogador ja tem em cena.
        /// </summary>
        public void Configure(float newMaxEnergy, float newThrustDrainPerSecond, float newIdleDrainPerSecond)
        {
            maxEnergy = Mathf.Max(1f, newMaxEnergy);
            thrustDrainPerSecond = Mathf.Max(0f, newThrustDrainPerSecond);
            idleDrainPerSecond = Mathf.Max(0f, newIdleDrainPerSecond);
            CurrentEnergy = Mathf.Clamp(CurrentEnergy, 0f, maxEnergy);
        }
    }
}
