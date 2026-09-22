using System;
using Lumen.Data;

namespace Lumen.Gameplay.Challenges
{
    /// <summary>
    /// Contrato comum para qualquer desafio de fase (quiz agora, minigames de
    /// Saturno/Jupiter depois). Cada implementacao chama onCompleted quando o
    /// jogador termina com sucesso - quem dispara o desafio (PlanetApproachTrigger)
    /// nao precisa saber qual tipo de desafio esta rodando por baixo.
    /// </summary>
    public interface IPhaseChallenge
    {
        void StartChallenge(PhaseData phase, Action onCompleted);
    }
}
