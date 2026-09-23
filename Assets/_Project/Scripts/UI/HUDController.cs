using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Gameplay;

namespace Lumen.UI
{
    /// <summary>
    /// HUD principal: barra de COMBUSTIVEL (drena enquanto o LUMEN se move),
    /// barra de SINAL com a Terra (sobe conforme fragmentos sao coletados),
    /// objetivo atual, contador de fragmentos e a seta de navegacao.
    /// O painel de quiz e a legenda da NOVA sao componentes separados.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private const int TotalFragments = 5; // Netuno, Urano, Saturno, Jupiter, Marte

        [Header("Barras (canto superior esquerdo)")]
        [SerializeField] private Slider signalBar;   // sinal com a Terra
        [SerializeField] private Slider energyBar;   // combustivel
        [SerializeField] private Text signalLabel;   // legenda acima: "SINAL"
        [SerializeField] private Text energyLabel;   // legenda acima: "COMBUSTIVEL"
        [SerializeField] private Text signalPercentText; // % DENTRO da barra
        [SerializeField] private Text energyPercentText; // % DENTRO da barra

        [Header("Textos")]
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private Text fragmentsLabel;

        [Header("Navegacao")]
        [SerializeField] private Text navArrow;

        // Preenchimento direto do fillAmount: o Slider do Unity pode nao renderizar
        // vazao de mudanca em alguns setups, entao dirigimos a Imagem Filled por
        // conta propria, alem de manter a barra sincronizada.
        private Image _signalFill;
        private Image _energyFill;

        private int _fragmentsCollected;
        private bool _tutorialDone;
        private float _tutorialCompleteBannerTimer;

        private Transform _navigationTarget;
        private string _targetName;
        private bool _navigationActive;
        private Camera _cam;

        private Transform _planetTransform;
        private float _signalRefDistance = 1f;
        private Transform _gateTransform;

        private void OnEnable()
        {
            EventBus.OnEnergyChanged += HandleEnergyChanged;
            EventBus.OnFragmentCollected += HandleFragmentCollected;
            EventBus.OnTutorialCompleted += HandleTutorialCompleted;
            EventBus.OnNavigateTo += HandleNavigateTo;
            EventBus.OnMissionComplete += HandleMissionComplete;
        }

        private void OnDisable()
        {
            EventBus.OnEnergyChanged -= HandleEnergyChanged;
            EventBus.OnFragmentCollected -= HandleFragmentCollected;
            EventBus.OnTutorialCompleted -= HandleTutorialCompleted;
            EventBus.OnNavigateTo -= HandleNavigateTo;
            EventBus.OnMissionComplete -= HandleMissionComplete;
        }

        private void Start()
        {
            if (signalLabel != null) signalLabel.text = "SINAL";
            if (energyLabel != null) energyLabel.text = "COMBUSTIVEL";
            if (objectiveLabel != null)
                objectiveLabel.text = "Use WASD para mover. Space / Ctrl para subir e descer. " +
                                      "Siga PARA A FRENTE ate o FAROL ao longe para concluir o tutorial.";

            // Referencia espacial do 1o planeta: usada para o sinal crescer
            // conforme a nave se aproxima (barra dinamica), e para o prompt do tutorial.
            var planet = Object.FindFirstObjectByType<PlanetApproachTrigger>();
            if (planet != null)
            {
                _planetTransform = planet.transform;

                Vector3 refPos = transform.position;
                var lumen = GameObject.Find("LUMEN");
                if (lumen != null) refPos = lumen.transform.position;
                _signalRefDistance = Mathf.Max(1f, Vector3.Distance(refPos, _planetTransform.position));
            }

            var gate = GameObject.Find("TutorialExit");
            if (gate != null) _gateTransform = gate.transform;

            UpdateSignalBar();
            UpdateFragmentsLabel();
        }

        private void LateUpdate()
        {
            // Sinal e combustivel sao recalculados a cada quadro: a barra de sinal
            // cresce conforme a nave se aproxima do planeta, entao nunca fica parada.
            UpdateSignalBar();

            // Durante o tutorial, mostra a distancia ate o farol de saida, deixando
            // muito claro que ele termina ao chegar la (nao e infinito).
            if (!_tutorialDone)
            {
                UpdateTutorialPrompt();
                return;
            }

            // Segura por alguns segundos o aviso de conclusao do tutorial
            // antes de a seta de navegacao voltar a atualizar o objetivo.
            if (_tutorialCompleteBannerTimer > 0f)
            {
                _tutorialCompleteBannerTimer -= Time.deltaTime;
                return;
            }

            UpdateNavigationArrow();
        }

        private void UpdateTutorialPrompt()
        {
            if (navArrow != null && navArrow.gameObject.activeSelf)
                navArrow.gameObject.SetActive(false);

            if (objectiveLabel == null || _gateTransform == null) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float dist = Vector3.Distance(_cam.transform.position, _gateTransform.position);
            objectiveLabel.text = "TUTORIAL\n" +
                                  "MOVIMENTO: WASD | SUBIR/DESCER: SPACE / CTRL\n" +
                                  $"Atravesse o FAROL a frente ({dist:F0} m) para concluir.";
        }

        private void HandleEnergyChanged(float current, float max)
        {
            if (energyBar == null) return;
            energyBar.maxValue = max;
            energyBar.value = current;
            SetFill(_energyFill, current, max);
            if (energyPercentText != null)
                energyPercentText.text = $"{Mathf.RoundToInt(current)}%";
        }

        private void HandleFragmentCollected(string planetName)
        {
            _fragmentsCollected++;
            UpdateFragmentsLabel();
            UpdateSignalBar();

            // Seta foi usada: esconde e prepara o proximo rumo (fases futuras).
            _navigationActive = false;
            _navigationTarget = null;
            if (navArrow != null) navArrow.gameObject.SetActive(false);
            if (objectiveLabel != null)
                objectiveLabel.text = $"Fragmento de {planetName} coletado! Energia restaurada.";
        }

        private void HandleTutorialCompleted()
        {
            _tutorialDone = true;
            UpdateSignalBar();

            // Aviso claro: o tutorial acabou e o jogo comecou. A seta e o boost
            // sao liberados juntos (o LUMEN escuta o mesmo evento).
            _tutorialCompleteBannerTimer = 4f;
            if (objectiveLabel != null)
                objectiveLabel.text = "TUTORIAL CONCLUIDO! Boost liberado (Shift). Siga a seta.";

            _navigationActive = true;

            // Primeiro destino: o planeta de menor RouteIndex (Netuno = 0).
            var start = GetRouteStart();
            if (start == null)
            {
                if (objectiveLabel != null)
                    objectiveLabel.text = "Tutorial concluido! Siga para o primeiro planeta.";
                return;
            }

            _navigationTarget = start.transform;
            _targetName = start.PhaseName;
        }

        private static PlanetApproachTrigger GetRouteStart()
        {
            PlanetApproachTrigger[] triggers =
                Object.FindObjectsByType<PlanetApproachTrigger>(FindObjectsSortMode.None);

            PlanetApproachTrigger best = null;
            foreach (var t in triggers)
            {
                if (t.RouteIndex < 0) continue;
                if (best == null || t.RouteIndex < best.RouteIndex) best = t;
            }

            return best;
        }

        private void HandleNavigateTo(Transform target, string displayName)
        {
            _navigationTarget = target;
            _targetName = displayName;
            _navigationActive = true;
        }

        private void HandleMissionComplete()
        {
            _navigationActive = false;
            _navigationTarget = null;
            if (navArrow != null) navArrow.gameObject.SetActive(false);
            if (objectiveLabel != null)
                objectiveLabel.text = "MISSAO CONCLUIDA! Bem-vinda de volta a Terra, LUMEN.";
        }

        /// <summary>
        /// Sinal com a Terra: comeca fraco (5%), ganha base ao concluir o tutorial,
        /// cresce em tempo real conforme a nave se aproxima do planeta e dispara a
        /// cada fragmento coletado, ate restaurar 100% de conexao.
        /// </summary>
        private void UpdateSignalBar()
        {
            if (signalBar == null) return;

            signalBar.maxValue = 100f;
            float signal = 5f;

            if (_tutorialDone) signal += 10f;

            if (_planetTransform != null && _signalRefDistance > 0f)
            {
                if (_cam == null) _cam = Camera.main;
                if (_cam != null)
                {
                    float dist = Vector3.Distance(_cam.transform.position, _planetTransform.position);
                    float proximity = Mathf.Clamp01(1f - dist / _signalRefDistance);
                    signal += proximity * 50f;
                }
            }

            signal += _fragmentsCollected * 15f;
            signal = Mathf.Max(0f, Mathf.Min(100f, signal));

            signalBar.value = signal;
            SetFill(_signalFill, signal, 100f);
            if (signalPercentText != null)
                signalPercentText.text = $"{Mathf.RoundToInt(signal)}%";
        }

        private static void SetFill(Image fill, float current, float max)
        {
            if (fill == null || max <= 0f) return;
            fill.fillAmount = Mathf.Clamp01(current / max);
        }

        private void UpdateNavigationArrow()
        {
            if (navArrow == null) return;

            bool playable = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Playing;
            bool show = _navigationActive && playable && _navigationTarget != null;

            if (!show)
            {
                if (navArrow.gameObject.activeSelf) navArrow.gameObject.SetActive(false);
                return;
            }

            if (!navArrow.gameObject.activeSelf) navArrow.gameObject.SetActive(true);
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            Vector3 toTarget = _navigationTarget.position - _cam.transform.position;

            if (objectiveLabel != null)
                objectiveLabel.text = $"Siga ate {_targetName} ({toTarget.magnitude:F0} m)";

            if (Vector3.Dot(toTarget, _cam.transform.forward) < 0f)
            {
                // Alvo atras da camera: seta aponta para baixo = "vire-se".
                navArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 180f);
                return;
            }

            Vector3 projected = Vector3.ProjectOnPlane(toTarget, _cam.transform.forward);
            Vector3 upPlane = Vector3.ProjectOnPlane(_cam.transform.up, _cam.transform.forward);

            float angle = projected.sqrMagnitude > 0.0001f
                ? Vector3.SignedAngle(upPlane, projected, _cam.transform.forward)
                : 0f;

            navArrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void UpdateFragmentsLabel()
        {
            if (fragmentsLabel != null)
                fragmentsLabel.text = $"Fragmentos: {_fragmentsCollected}/{TotalFragments}";
        }

        /// <summary>Usado pelo TutorialSceneBuilder para ligar as referencias de UI sem reflection.</summary>
        public void Configure(Slider energy, Slider signal, Text objective, Text fragments, Text sigLabel, Text fuelLabel,
            Text fuelPercent, Text sigPercent)
        {
            energyBar = energy;
            signalBar = signal;
            objectiveLabel = objective;
            fragmentsLabel = fragments;
            signalLabel = sigLabel;
            energyLabel = fuelLabel;
            energyPercentText = fuelPercent;
            signalPercentText = sigPercent;

            // Resolve as imagens de preenchimento para dirigir o fillAmount direto.
            _signalFill = ResolveFill(signal);
            _energyFill = ResolveFill(energy);
        }

        private static Image ResolveFill(Slider bar)
        {
            if (bar == null) return null;
            var fill = bar.transform.Find("Fill");
            if (fill == null) return null;

            var img = fill.GetComponent<Image>();
            if (img == null) return null;

            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = 0;
            return img;
        }

        /// <summary>Liga a seta de navegacao (comeca escondida ate o tutorial terminar).</summary>
        public void SetupNavigation(Text arrow)
        {
            navArrow = arrow;
            if (navArrow != null) navArrow.gameObject.SetActive(false);
        }
    }
}