using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Controla a energia/combustivel do LUMEN. Na Fase 1 (Tutorial) o dreno e
    /// propositalmente suave: o GameDoc descreve essa fase como uma zona segura,
    /// "sem pressao de tempo". A partir da Fase 2, o Refill() sera chamado pelo
    /// sistema de desafios ao coletar um fragmento de conhecimento.
    /// </summary>
    public class LumenEnergySystem : MonoBehaviour
    {
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float idleDrainPerSecond = 0.6f;
        [SerializeField] private float thrustDrainPerSecond = 2.2f;

        public float CurrentEnergy { get; private set; }
        public float MaxEnergy => maxEnergy;

        private void Start()
        {
            CurrentEnergy = maxEnergy;
            EventBus.RaiseEnergyChanged(CurrentEnergy, maxEnergy);
        }

        public void Tick(float deltaTime, bool isThrusting)
        {
            float drain = isThrusting ? thrustDrainPerSecond : idleDrainPerSecond;
            CurrentEnergy = Mathf.Max(0f, CurrentEnergy - drain * deltaTime);
            EventBus.RaiseEnergyChanged(CurrentEnergy, maxEnergy);
        }

        public void Refill(float amount)
        {
            CurrentEnergy = Mathf.Min(maxEnergy, CurrentEnergy + amount);
            EventBus.RaiseEnergyChanged(CurrentEnergy, maxEnergy);
        }
    }
}
