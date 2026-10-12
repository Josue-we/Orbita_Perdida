using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Gameplay;

namespace Lumen.UI
{
    /// <summary>
    /// HUD principal: barra de COMBUSTÍVEL (drena com os propulsores acesos e
    /// zera = LUMEN parado + resgate), barra de SINAL com a Terra (restabelecido
    /// a cada fragmento coletado), objetivo atual, contador de fragmentos e a
    /// seta de navegacao. O painel de quiz e a legenda da NOVA sao componentes
    /// separados.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        private const int TotalFragments = 5; // Netuno, Urano, Saturno, Júpiter, Marte

        // Abaixo disso o combustivel vira um aviso no objetivo, para o jogador
        // saber que a nave esta prestes a parar (e nao entender do nada).
        private const float LowFuelThreshold = 0.25f;
        private const float RescueBannerSeconds = 4f;

        // Velocidade (em pontos de % por segundo) com que a barra de sinal sobe
        // ate o novo valor: cada fragmento "restabelece" o sinal a olhos vistos.
        private const float SignalRiseSpeed = 30f;

        [Header("Barras (canto superior esquerdo)")]
        [SerializeField] private Slider signalBar;   // sinal com a Terra
        [SerializeField] private Slider energyBar;   // combustivel
        [SerializeField] private Text signalLabel;   // legenda acima: "SINAL"
        [SerializeField] private Text energyLabel;   // legenda acima: "COMBUSTÍVEL"
        [SerializeField] private Text signalPercentText; // % DENTRO da barra
        [SerializeField] private Text energyPercentText; // % DENTRO da barra

        [Header("Textos")]
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private Text fragmentsLabel;

        [Header("Navegação")]
        [SerializeField] private Text navArrow;

        // Preenchimento direto do fillAmount: o Slider do Unity pode nao renderizar
        // vazao de mudanca em alguns setups, entao dirigimos a Imagem Filled por
        // conta propria, alem de manter a barra sincronizada.
        private Image _signalFill;
        private Image _energyFill;

        private int _fragmentsCollected;
        private bool _tutorialDone;
        private bool _missionComplete;
        private float _displayedSignal = GameTuning.SignalBase;
        private float _tutorialCompleteBannerTimer;
        private float _rescueBannerTimer;

        // Combustivel atual: o HUD deduz sozinho os avisos de tanque baixo/zerado
        // a partir do que o LumenEnergySystem ja publica no EventBus.
        private float _energy;
        private float _maxEnergy;

        private Transform _navigationTarget;
        private string _targetName;
        private bool _navigationActive;
        private Camera _cam;

        private Transform _gateTransform;

        private void Awake()
        {
            ApplyTextTuning();
            BuildArrowGraphic();
        }

        /// <summary>
        /// Troca o triangulo de texto (todos os lados iguais, sem direcao clara) por uma
        /// SETA de verdade: haste + ponta, com contorno. O Text antigo fica desligado e a seta
        /// gira junto com o mesmo objeto, entao nada mais na logica muda.
        /// </summary>
        private void BuildArrowGraphic()
        {
            if (navArrow == null) return;

            navArrow.enabled = false;
            var host = navArrow.rectTransform;
            host.sizeDelta = new Vector2(40f, 54f);

            var existing = host.Find("ArrowGraphic");
            if (existing != null) return;

            var go = new GameObject("ArrowGraphic", typeof(RectTransform));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.sprite = CreateArrowSprite();
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = new Color(0.62f, 0.93f, 1f, 0.78f);

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0.05f, 0.1f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }

        /// <summary>
        /// Seta SUAVE apontando para cima, desenhada por codigo: tracos finos com pontas
        /// arredondadas (haste + "v" da ponta), com borda anti-serrilhada.
        /// </summary>
        private static Sprite CreateArrowSprite()
        {
            const int w = 96, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var tip = new Vector2(48f, 108f);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Min3(
                        DistToSegment(p, new Vector2(48f, 22f), tip),          // haste
                        DistToSegment(p, tip, new Vector2(16f, 66f)),          // asa esquerda
                        DistToSegment(p, tip, new Vector2(80f, 66f)));         // asa direita

                    const float radius = 7f;
                    float alpha = Mathf.Clamp01(radius + 0.5f - d);            // 1px de suavizacao
                    pixels[y * w + x] = new Color32(255, 255, 255, (byte)(255f * alpha));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        private static float Min3(float a, float b, float c) => Mathf.Min(a, Mathf.Min(b, c));

        /// <summary>Fontes maiores aplicadas em runtime (valem em cena ja montada).</summary>
        private void ApplyTextTuning()
        {
            // Barras e legendas maiores, empilhadas no canto superior esquerdo.
            UiTextTuning.SetAnchors(signalLabel != null ? signalLabel.rectTransform : null, new Vector2(0.02f, 0.945f), new Vector2(0.34f, 0.99f));
            UiTextTuning.SetAnchors(signalBar != null ? signalBar.transform as RectTransform : null, new Vector2(0.02f, 0.885f), new Vector2(0.34f, 0.94f));
            UiTextTuning.SetAnchors(energyLabel != null ? energyLabel.rectTransform : null, new Vector2(0.02f, 0.825f), new Vector2(0.34f, 0.87f));
            UiTextTuning.SetAnchors(energyBar != null ? energyBar.transform as RectTransform : null, new Vector2(0.02f, 0.765f), new Vector2(0.34f, 0.82f));
            if (fragmentsLabel != null)
                UiTextTuning.SetAnchors(fragmentsLabel.rectTransform, new Vector2(0.60f, 0.92f), new Vector2(0.98f, 0.99f));

            UiTextTuning.Apply(signalLabel, GameTuning.HudLabelFontSize);
            UiTextTuning.Apply(energyLabel, GameTuning.HudLabelFontSize);
            UiTextTuning.Apply(signalPercentText, GameTuning.HudPercentFontSize, 12);
            UiTextTuning.Apply(energyPercentText, GameTuning.HudPercentFontSize, 12);
            UiTextTuning.Apply(fragmentsLabel, GameTuning.HudFragmentsFontSize);

            if (objectiveLabel != null)
            {
                // Um pouco mais alta para caber ate 4 linhas com a fonte maior.
                UiTextTuning.SetAnchors(objectiveLabel.rectTransform, new Vector2(0.02f, 0.02f), new Vector2(0.88f, 0.17f));
                UiTextTuning.Apply(objectiveLabel, GameTuning.HudObjectiveFontSize);
            }
        }

        private void OnEnable()
        {
            EventBus.OnEnergyChanged += HandleEnergyChanged;
            EventBus.OnFragmentCollected += HandleFragmentCollected;
            EventBus.OnTutorialCompleted += HandleTutorialCompleted;
            EventBus.OnNavigateTo += HandleNavigateTo;
            EventBus.OnMissionComplete += HandleMissionComplete;
            EventBus.OnRescued += HandleRescued;
        }

        private void OnDisable()
        {
            EventBus.OnEnergyChanged -= HandleEnergyChanged;
            EventBus.OnFragmentCollected -= HandleFragmentCollected;
            EventBus.OnTutorialCompleted -= HandleTutorialCompleted;
            EventBus.OnNavigateTo -= HandleNavigateTo;
            EventBus.OnMissionComplete -= HandleMissionComplete;
            EventBus.OnRescued -= HandleRescued;
        }

        private void Start()
        {
            if (signalLabel != null) signalLabel.text = "SINAL";
            if (energyLabel != null) energyLabel.text = "COMBUSTÍVEL";
            if (objectiveLabel != null)
                objectiveLabel.text = "W/S para avançar e recuar, A/D para girar. Espaço / Ctrl para subir e descer. " +
                                      "X para frear. " +
                                      "Siga PARA A FRENTE até o FAROL ao longe para concluir o tutorial.";

            var gate = GameObject.Find("TutorialExit");
            if (gate != null) _gateTransform = gate.transform;

            _displayedSignal = TargetSignal;
            UpdateSignalBar();
            UpdateFragmentsLabel();
        }

        private void LateUpdate()
        {
            // O sinal anima ate o valor-alvo (que so cresce com os fragmentos).
            UpdateSignalBar();

            // Combustivel zerado: o LUMEN esta parado esperando o resgate, e isso
            // tem prioridade sobre qualquer outro texto da tela.
            if (IsOutOfFuel)
            {
                ShowOutOfFuelPrompt();
                return;
            }

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

            // Mesma coisa com o aviso de resgate, para o jogador ver o que houve
            // antes da seta voltar a apontar o proximo destino.
            if (_rescueBannerTimer > 0f)
            {
                _rescueBannerTimer -= Time.deltaTime;
                return;
            }

            UpdateNavigationArrow();
        }

        private bool IsOutOfFuel => _maxEnergy > 0f && _energy <= 0f;

        private void ShowOutOfFuelPrompt()
        {
            if (objectiveLabel == null) return;

            objectiveLabel.text = "COMBUSTÍVEL ESGOTADO! O LUMEN ficou sem propelentes.\n" +
                                  $"Pressione {LumenController.RescueKey} para voltar ao último planeta e seguir para o próximo.";
        }

        private void UpdateTutorialPrompt()
        {
            if (navArrow != null && navArrow.gameObject.activeSelf)
                navArrow.gameObject.SetActive(false);

            if (objectiveLabel == null || _gateTransform == null) return;

            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;

            float dist = Vector3.Distance(_cam.transform.position, _gateTransform.position);
            SetObjective("TUTORIAL\n" +
                         "AVANÇAR: W/S | GIRAR: A/D | SUBIR/DESCER: ESPAÇO / CTRL\n" +
                         $"Atravesse o FAROL à frente ({dist:F0} m) para concluir.");
        }

        /// <summary>
        /// Escreve o objetivo e pendura o aviso de combustivel baixo, para o
        /// jogador nunca ser pego de surpresa pela parada obrigatoria.
        /// </summary>
        private void SetObjective(string line)
        {
            if (objectiveLabel == null) return;

            if (_maxEnergy > 0f && _energy / _maxEnergy <= LowFuelThreshold)
                line += $"\nCOMBUSTÍVEL BAIXO ({Mathf.RoundToInt(_energy / _maxEnergy * 100f)}%) - cada fragmento coletado reabastece.";

            objectiveLabel.text = line;
        }

        private void HandleEnergyChanged(float current, float max)
        {
            _energy = current;
            _maxEnergy = max;

            if (energyBar == null) return;
            energyBar.maxValue = max;
            energyBar.value = current;
            SetFill(_energyFill, current, max);
            if (energyPercentText != null)
                energyPercentText.text = $"{Mathf.RoundToInt(current)}%";
        }

        private void HandleRescued(string place)
        {
            _rescueBannerTimer = RescueBannerSeconds;
            if (objectiveLabel != null)
                objectiveLabel.text = $"RESGATE! O LUMEN voltou para {place} com o tanque cheio.";
        }

        private void HandleFragmentCollected(string planetName)
        {
            _fragmentsCollected++;
            UpdateFragmentsLabel();

            // Seta foi usada: esconde e prepara o proximo rumo (fases futuras).
            _navigationActive = false;
            _navigationTarget = null;
            if (navArrow != null) navArrow.gameObject.SetActive(false);
            SetObjective($"Fragmento de {planetName} coletado! Energia restaurada e sinal com a Terra mais forte.");
        }

        private void HandleTutorialCompleted()
        {
            _tutorialDone = true;

            // Aviso claro: o tutorial acabou e o jogo comecou. A seta e o boost
            // sao liberados juntos (o LUMEN escuta o mesmo evento).
            _tutorialCompleteBannerTimer = 4f;
            if (objectiveLabel != null)
                objectiveLabel.text = "TUTORIAL CONCLUÍDO! Boost liberado (Shift). Siga a seta.";

            _navigationActive = true;

            // Primeiro destino: o planeta de menor RouteIndex (Netuno = 0).
            var start = GetRouteStart();
            if (start == null)
            {
                SetObjective("Tutorial concluído! Siga para o primeiro planeta.");
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
            _missionComplete = true;
            _navigationActive = false;
            _navigationTarget = null;
            if (navArrow != null) navArrow.gameObject.SetActive(false);
            SetObjective("MISSÃO CONCLUÍDA! Bem-vindo de volta à Terra, LUMEN.");
        }

        /// <summary>
        /// Valor-alvo do sinal com a Terra. NAO depende de distancia: so cresce,
        /// 5% no inicio + 18% a cada fragmento coletado (95% com os 5 fragmentos)
        /// e 100% ao concluir a missao. Como o alvo nunca diminui, a barra tambem nunca.
        /// </summary>
        private float TargetSignal =>
            _missionComplete
                ? 100f
                : Mathf.Min(100f, GameTuning.SignalBase + _fragmentsCollected * GameTuning.SignalPerFragment);

        private void UpdateSignalBar()
        {
            if (signalBar == null) return;

            // MoveTowards em direcao a um alvo que so sobe: animacao suave, sem recuar.
            _displayedSignal = Mathf.MoveTowards(_displayedSignal, TargetSignal,
                SignalRiseSpeed * Time.unscaledDeltaTime);

            signalBar.maxValue = 100f;
            signalBar.value = _displayedSignal;
            SetFill(_signalFill, _displayedSignal, 100f);
            if (signalPercentText != null)
                signalPercentText.text = $"{Mathf.RoundToInt(_displayedSignal)}%";
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

            SetObjective($"Siga até {_targetName} ({toTarget.magnitude:F0} m)");

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
