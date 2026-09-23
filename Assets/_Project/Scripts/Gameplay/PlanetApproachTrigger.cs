using UnityEngine;
using Lumen.Core;
using Lumen.Data;
using Lumen.Narrative;
using Lumen.Gameplay.Challenges;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Colocado num planeta: quando o LUMEN entra, trava o movimento, toca a
    /// narracao da NOVA e em seguida o desafio (quiz, ou futuramente um
    /// minigame). Ao terminar, converte o fragmento em energia e libera o
    /// movimento de novo. Dispara uma unica vez - por colisao OU por checagem
    /// de distancia (fallback que nao depende de fisica nem de tag), para
    /// garantir que o quiz da primeira fase sempre apareca.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlanetApproachTrigger : MonoBehaviour
    {
        [SerializeField] private PhaseData phase;
        [SerializeField] private Transform nextPlanet;   // proximo da rota (null = destino final)
        [SerializeField] private int routeIndex = -1;    // ordem da rota (0 = Netuno ...)

        /// <summary>Nome do planeta/fase - usado pelo HUD para a seta e o objetivo.</summary>
        public string PhaseName => phase == null ? name : phase.PlanetName;

        /// <summary>Ordem da rota - o HUD usa para achar o primeiro destino apos o tutorial.</summary>
        public int RouteIndex => routeIndex;

        private bool _triggered;
        private Transform _player;
        private float _checkTimer;

        private float WorldTriggerRadius
        {
            get
            {
                float scale = Mathf.Max(transform.lossyScale.x, 0.001f);
                var col = GetComponent<Collider>();
                return col is SphereCollider sphere ? sphere.radius * scale : 0f;
            }
        }

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

            float radius = WorldTriggerRadius;
            if (radius <= 0f) return;

            // Reforco: mesmo que o OnTriggerEnter nunca dispare, ao se aproximar
            // da zona de encontro a fase começa.
            if (Vector3.Distance(transform.position, _player.position) <= radius + 2f)
                StartEncounter(_player);
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
            StartEncounter(other.transform);
        }

        private void StartEncounter(Transform playerTransform)
        {
            if (_triggered) return;

            if (phase == null)
            {
                Debug.LogWarning($"[LUMEN] {name} nao tem PhaseData atribuido - ignorando encontro.");
                return;
            }

            _triggered = true;
            GameManager.Instance.SetState(GameState.Challenge);

            // Garante que o tutorial terminou antes do quiz aparecer. Se o jogador
            // chegou aqui sem cruzar a porta do tutorial, o tutorial "finaliza" agora -
            // nunca fica preso num tutorial infinito.
            EventBus.RaiseTutorialCompleted();

            if (playerTransform == null) _player = FindPlayer();
            var energy = _player != null ? _player.GetComponent<LumenEnergySystem>() : null;
            var narrator = Object.FindFirstObjectByType<NovaNarrationController>();

            if (narrator != null && phase.NarrationLines != null && phase.NarrationLines.Length > 0)
                narrator.Play(phase.NarrationLines, () => StartChallenge(energy));
            else
                StartChallenge(energy);
        }

        private void StartChallenge(LumenEnergySystem energy)
        {
            var quiz = Object.FindFirstObjectByType<QuizChallengeController>();
            if (quiz != null)
                quiz.StartChallenge(phase, () => CompletePhase(energy));
            else
                CompletePhase(energy);
        }

        private void CompletePhase(LumenEnergySystem energy)
        {
            // Ultimo planeta da rota (nextPlanet nulo): missao concluida, sem fragmento.
            if (nextPlanet == null)
            {
                EventBus.RaiseMissionComplete();
                GameManager.Instance.SetState(GameState.Playing);
                return;
            }

            EventBus.RaiseFragmentCollected(phase.PlanetName);
            if (energy != null) energy.Refill(phase.FragmentEnergyReward);

            GameManager.Instance.SetState(GameState.Playing);

            // Seta de navegacao aponta para o proximo planeta da rota.
            var nextTrigger = nextPlanet != null
                ? nextPlanet.GetComponent<PlanetApproachTrigger>()
                : null;
            if (nextTrigger != null)
                EventBus.RaiseNavigateTo(nextPlanet, nextTrigger.PhaseName);
        }

        /// <summary>Usado pelo TutorialSceneBuilder para atribuir o PhaseData sem reflection.</summary>
        public void SetPhase(PhaseData phaseData) => phase = phaseData;

        /// <summary>Usado pelo TutorialSceneBuilder para encadear a rota planeta a planeta.</summary>
        public void SetNextPlanet(Transform planet) => nextPlanet = planet;

        /// <summary>Usado pelo TutorialSceneBuilder para definir a ordem da rota (0 = primeiro destino).</summary>
        public void SetRouteIndex(int index) => routeIndex = index;
    }
}