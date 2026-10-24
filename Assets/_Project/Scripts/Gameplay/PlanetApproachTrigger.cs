using System.Collections;
using UnityEngine;
using Lumen.Core;
using Lumen.Cameras;
using Lumen.Gameplay.Effects;
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

        // Direcao (no plano horizontal) da nave para o planeta, usada pela camera de orbita
        // e para posicionar o fragmento de conhecimento.
        private Vector3 _towardPlanet = Vector3.forward;
        private CameraFollow _cameraFollow;

        private static readonly string[] CelebrationLines =
        {
            "Muito bem! Fragmento de conhecimento recebido.",
            "Isso! Energia restaurada, LUMEN.",
            "Excelente! O sinal com a Terra está mais forte.",
            "Ótima resposta! Seguimos viagem.",
            "Perfeito! Mais um fragmento a bordo.",
        };

        private float WorldTriggerRadius
        {
            get
            {
                float scale = Mathf.Max(transform.lossyScale.x, 0.001f);
                var col = GetComponent<Collider>();
                return col is SphereCollider sphere ? sphere.radius * scale : 0f;
            }
        }

        /// <summary>So o proximo planeta da rota pode abrir o encontro (sem pular etapas).</summary>
        private bool IsNextInRoute => routeIndex < 0 || routeIndex == EventBus.RouteProgress;

        private void Update()
        {
            if (_triggered) return;
            if (!IsNextInRoute) return;

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
            if (!IsNextInRoute) return;
            if (!IsPlayer(other)) return;
            StartEncounter(other.transform);
        }

        private void StartEncounter(Transform playerTransform)
        {
            if (_triggered) return;
            if (!IsNextInRoute) return;

            if (phase == null)
            {
                Debug.LogWarning($"[LUMEN] {name} não tem PhaseData atribuído - ignorando encontro.");
                return;
            }

            _triggered = true;
            GameManager.Instance.SetState(GameState.Challenge);

            // Garante que o tutorial terminou antes do quiz aparecer. Se o jogador
            // chegou aqui sem cruzar a porta do tutorial, o tutorial "finaliza" agora -
            // nunca fica preso num tutorial infinito.
            EventBus.RaiseTutorialCompleted();

            // Sempre a raiz do LUMEN (o colisor que entrou pode ser um filho).
            _player = FindPlayer();
            var energy = _player != null ? _player.GetComponent<LumenEnergySystem>() : null;
            var narrator = Object.FindFirstObjectByType<NovaNarrationController>();

            // Entrada na orbita: a camera vai para um angulo cinematografico e a nave desacelera
            // enquanto a NOVA diz a fala curta ("Estamos entrando na orbita de ...").
            _cameraFollow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (_player != null) StartCoroutine(OrbitEntrance());

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
            if (routeIndex >= 0) EventBus.AdvanceRoute(routeIndex);

            // Ultimo planeta da rota (nextPlanet nulo): missao concluida, sem fragmento.
            if (nextPlanet == null)
            {
                EventBus.RaiseMissionComplete();
                GameManager.Instance.SetState(GameState.Playing);
                if (_cameraFollow != null) _cameraFollow.EndCinematic();
                return;
            }

            StartCoroutine(CollectSequence(energy));
        }

        /// <summary>
        /// Camera cinematografica ao lado da nave, olhando para o planeta, enquanto a nave
        /// desliza e para (velocidade cai suavemente a zero).
        /// </summary>
        private IEnumerator OrbitEntrance()
        {
            Transform ship = _player;
            var lumen = ship.GetComponent<LumenController>();

            Vector3 toPlanet = transform.position - ship.position;
            toPlanet.y = 0f;
            _towardPlanet = toPlanet.sqrMagnitude > 0.01f ? toPlanet.normalized : ship.forward;

            float speed0 = lumen != null ? Mathf.Max(0f, lumen.ForwardSpeed) : 0f;
            float total = GameTuning.OrbitGlideSeconds;
            float t = 0f;

            while (t < total && ship != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(1f - t / total);
                ship.position += ship.forward * (speed0 * k * k * Time.deltaTime);
                UpdateOrbitCamera(ship);
                yield return null;
            }

            if (lumen != null) lumen.StopMotion();
            if (ship != null) UpdateOrbitCamera(ship);
        }

        private void UpdateOrbitCamera(Transform ship)
        {
            if (_cameraFollow == null) return;

            Vector3 right = Vector3.Cross(Vector3.up, _towardPlanet);
            Vector3 position = ship.position + right * GameTuning.OrbitCamSide
                               + Vector3.up * GameTuning.OrbitCamUp
                               - _towardPlanet * GameTuning.OrbitCamBack;
            Vector3 lookAt = ship.position + _towardPlanet * GameTuning.OrbitLookAhead + Vector3.up * 3f;
            _cameraFollow.SetCinematic(position, lookAt);
        }

        /// <summary>
        /// Acertou: o fragmento brilhante surge perto do LUMEN, e atraido ate ele, explode em
        /// particulas douradas e so entao conta como coletado (som, HUD, energia). A NOVA comemora
        /// com uma fala curta e o jogador volta a voar.
        /// </summary>
        private IEnumerator CollectSequence(LumenEnergySystem energy)
        {
            Transform ship = _player != null ? _player : FindPlayer();
            Vector3 right = Vector3.Cross(Vector3.up, _towardPlanet);

            if (ship != null)
            {
                Vector3 start = ship.position + _towardPlanet * 40f + Vector3.up * 7f + right * 8f;
                yield return FragmentCollectEffect.FlyTo(ship, start, () => OnFragmentArrived(energy));
            }
            else
            {
                OnFragmentArrived(energy);
            }

            yield return new WaitForSeconds(GameTuning.FragmentResumeDelay);

            GameManager.Instance.SetState(GameState.Playing);
            if (_cameraFollow != null) _cameraFollow.EndCinematic();

            // Seta de navegacao aponta para o proximo planeta da rota.
            var nextTrigger = nextPlanet != null
                ? nextPlanet.GetComponent<PlanetApproachTrigger>()
                : null;
            if (nextTrigger != null)
                EventBus.RaiseNavigateTo(nextPlanet, nextTrigger.PhaseName);
        }

        private void OnFragmentArrived(LumenEnergySystem energy)
        {
            EventBus.RaiseFragmentCollected(phase.PlanetName);
            if (energy != null) energy.Refill(phase.FragmentEnergyReward);

            // O planeta recem-resolvido vira o novo ponto de resgate: se o
            // combustivel zerar no proximo trecho, o LUMEN volta para aqui.
            GameManager.Instance.SetRescuePoint(GetRescuePosition(), phase.PlanetName);

            // NOVA comemora com uma fala curta (nao some se o jogador apertar uma tecla).
            var narrator = Object.FindFirstObjectByType<NovaNarrationController>();
            if (narrator != null)
            {
                int i = Mathf.Abs(routeIndex) % CelebrationLines.Length;
                narrator.Play(new[] { CelebrationLines[i] }, null, false);
            }
        }

        /// <summary>
        /// Ponto de resgate logo acima do planeta e FORA da zona de encontro, para
        /// a nave resgatada comecar o proximo trecho limpa (sem reentrar no quiz).
        /// </summary>
        public Vector3 GetRescuePosition()
        {
            return transform.position + Vector3.up * (WorldTriggerRadius + 40f);
        }

        /// <summary>Usado pelo TutorialSceneBuilder para atribuir o PhaseData sem reflection.</summary>
        public void SetPhase(PhaseData phaseData) => phase = phaseData;

        /// <summary>Usado pelo TutorialSceneBuilder para encadear a rota planeta a planeta.</summary>
        public void SetNextPlanet(Transform planet) => nextPlanet = planet;

        /// <summary>Usado pelo TutorialSceneBuilder para definir a ordem da rota (0 = primeiro destino).</summary>
        public void SetRouteIndex(int index) => routeIndex = index;
    }
}