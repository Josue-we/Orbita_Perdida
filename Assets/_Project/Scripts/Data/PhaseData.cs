using UnityEngine;

namespace Lumen.Data
{
    /// <summary>
    /// Descreve uma fase/planeta: o que a NOVA fala na aproximacao, qual quiz
    /// usar, e quanta energia o fragmento devolve. Cada novo planeta e so uma
    /// nova instancia deste asset - nenhum codigo novo e necessario. Quando
    /// os minigames de Saturno e Jupiter forem construidos, este mesmo asset
    /// pode ganhar um campo de configuracao de minigame, sem quebrar nada
    /// que ja existe.
    /// </summary>
    [CreateAssetMenu(fileName = "NewPhaseData", menuName = "LUMEN/Phase Data")]
    public class PhaseData : ScriptableObject
    {
        public string PlanetName;

        [TextArea(2, 4)]
        public string[] NarrationLines;

        public QuizQuestionData Quiz;

        public float FragmentEnergyReward = 30f;
    }
}
