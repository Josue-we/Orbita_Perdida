using System.Collections.Generic;
using UnityEngine;

namespace Lumen.Data
{
    /// <summary>
    /// Descreve uma fase/planeta: o que a NOVA fala na aproximacao, quais quizzes
    /// podem cair e quanta energia o fragmento devolve. Cada novo planeta e so uma
    /// nova instancia deste asset - nenhum codigo novo e necessario.
    ///
    /// QuizPool guarda VARIAS perguntas do planeta. A cada chegada o jogo sorteia
    /// uma (PickQuiz), sem repetir ate todas terem aparecido - e a memoria vale
    /// entre partidas (PlayerPrefs), entao jogar de novo nao traz a mesma pergunta.
    /// Se o pool estiver vazio, usa o campo Quiz (comportamento antigo).
    /// </summary>
    [CreateAssetMenu(fileName = "NewPhaseData", menuName = "LUMEN/Phase Data")]
    public class PhaseData : ScriptableObject
    {
        private const string SeenKeyPrefix = "lumen.quizseen.";

        public string PlanetName;

        [TextArea(2, 4)]
        public string[] NarrationLines;

        [Tooltip("Pergunta unica (fallback). Usada quando o Quiz Pool esta vazio.")]
        public QuizQuestionData Quiz;

        [Tooltip("Varias perguntas do planeta. Uma e sorteada a cada chegada, sem repetir ate acabarem.")]
        public QuizQuestionData[] QuizPool;

        // 75 = o pior trecho da rota custa ~80, mas o fragmento sempre devolve o
        // bastante para a proxima perna (o tanque enche ate 100).
        public float FragmentEnergyReward = 75f;

        /// <summary>Sorteia a pergunta desta chegada, sem repetir ate o pool inteiro ser usado.</summary>
        public QuizQuestionData PickQuiz()
        {
            var valid = new List<int>();
            if (QuizPool != null)
            {
                for (int i = 0; i < QuizPool.Length; i++)
                    if (QuizPool[i] != null) valid.Add(i);
            }

            if (valid.Count == 0) return Quiz;
            if (valid.Count == 1) return QuizPool[valid[0]];

            string key = SeenKeyPrefix + PlanetName;
            List<int> seen = ParseSeen(PlayerPrefs.GetString(key, ""));

            List<int> candidates = valid.FindAll(i => !seen.Contains(i));
            if (candidates.Count == 0)
            {
                // Todas ja apareceram: recomeca o ciclo, so evitando repetir a ultima.
                int last = seen.Count > 0 ? seen[seen.Count - 1] : -1;
                seen.Clear();
                candidates = valid.FindAll(i => i != last);
                if (candidates.Count == 0) candidates = valid;
            }

            int pick = candidates[Random.Range(0, candidates.Count)];
            seen.Add(pick);

            PlayerPrefs.SetString(key, string.Join(",", seen));
            PlayerPrefs.Save();
            return QuizPool[pick];
        }

        private static List<int> ParseSeen(string raw)
        {
            var result = new List<int>();
            if (string.IsNullOrEmpty(raw)) return result;

            foreach (string part in raw.Split(','))
            {
                if (int.TryParse(part, out int value)) result.Add(value);
            }
            return result;
        }
    }
}
