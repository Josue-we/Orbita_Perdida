using UnityEngine;

namespace Lumen.Data
{
    /// <summary>
    /// Uma pergunta de quiz associada a um planeta. Crie novas via
    /// Assets > Create > LUMEN > Quiz Question, ou deixe o construtor de
    /// cena criar uma automaticamente na primeira vez que rodar.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuizQuestion", menuName = "LUMEN/Quiz Question")]
    public class QuizQuestionData : ScriptableObject
    {
        [TextArea(2, 4)]
        public string Question;

        public string[] Options = new string[2];

        public int CorrectIndex;

        [TextArea(1, 3)]
        public string CorrectFeedback = "Isso mesmo!";

        [TextArea(1, 3)]
        public string IncorrectFeedback = "Nao e bem isso. Tente de novo.";
    }
}
