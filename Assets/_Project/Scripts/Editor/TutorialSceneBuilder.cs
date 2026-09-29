#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Lumen.Core;
using Lumen.Gameplay;
using Lumen.Gameplay.Challenges;
using Lumen.Cameras;
using Lumen.UI;
using Lumen.Audio;
using Lumen.Environment;
using Lumen.Narrative;
using Lumen.Data;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor que monta a cena inteira automaticamente: GameManager,
    /// LUMEN, camera, HUD, menu inicial, menu de saida (Esc), intro, skybox,
    /// cenario decorativo, audio, narracao da NOVA, quiz e a rota de planetas.
    /// Use o menu "LUMEN > Montar Cena de Tutorial" - e seguro rodar mais de uma
    /// vez, cada parte confere se ja existe antes de criar de novo.
    /// </summary>
    public static class TutorialSceneBuilder
    {
        private const string PlanetsPackPath = "Assets/Planets of the Solar System 3D";
        private const string DataFolder = "Assets/_Project/Data";

        // Raio da zona de encontro: proporcional ao tamanho do planeta, com
        // minimo para nao disparar longe demais de planetas pequenos. Reduzido
        // de 1.7 para 1.4 (ainda comodo, mas permite planetas mais proximos
        // sem as zonas de quiz se sobreporem).
        private const float EncounterFactor = 1.4f;
        private const float MinEncounterRadius = 25f;

        // Rota recalibrada para ser mais curta (menos tempo "andando no espaço"):
        // a distancia minima entre dois planetas e limitada pelo tamanho deles
        // (Saturno e Jupiter sao enormes, entao a zona de encontro ao redor deles
        // tambem e grande - nao da pra encostar demais sem sobrepor as zonas de
        // quiz). Estas posicoes respeitam essa distancia minima com uma margem de
        // seguranca, resultando em ~5.400 unidades de rota total (contra ~8.600
        // da versao anterior, uma reducao de ~37%).
        private static readonly Vector3 LumenStartPosition = new Vector3(0f, 0f, 40f);
        private static readonly Vector3 TutorialExitPosition = new Vector3(0f, 0f, 120f);
        private static readonly Vector3 NetunoPosition = new Vector3(0f, 10f, 520f);
        private static readonly Vector3 UranoPosition = new Vector3(550f, 25f, 1100f);
        private static readonly Vector3 SaturnoPosition = new Vector3(0f, 45f, 2110f);
        private static readonly Vector3 JupiterPosition = new Vector3(-900f, 70f, 3433f);
        private static readonly Vector3 MartePosition = new Vector3(-200f, 40f, 4215f);
        private static readonly Vector3 TerraPosition = new Vector3(-300f, 20f, 4550f);

        // Todos os prefabs de planeta do pacote guardam escala 1 usando a mesma
        // malha esferica, entao por padrao todos apareceriam do mesmo tamanho.
        // Aqui aplicamos escala proporcional ao raio REAL de cada planeta, com
        // Netuno ancorado em 180. Formula: scale = 180 * raioRealKm / 24622.
        private static readonly System.Collections.Generic.Dictionary<string, float> PlanetScaleByPrefab =
            new System.Collections.Generic.Dictionary<string, float>
            {
                ["Neptune.prefab"] = 180.00f,
                ["Uranus.prefab"]  = 185.41f,
                ["Saturn.prefab"]  = 425.65f,
                ["Jupiter.prefab"] = 511.08f,
                ["Mars.prefab"]    = 24.78f,
                ["Earth.prefab"]   = 46.57f,
                ["Venus.prefab"]   = 44.24f,
                ["Mercury.prefab"] = 17.84f,
                ["Moon.prefab"]    = 12.70f,
                ["Pluto.prefab"]   = 8.69f,
            };

        [MenuItem("LUMEN/Montar Cena de Tutorial")]
        public static void BuildTutorialScene()
        {
            EnsureGameManager();
            EnsureEventSystem();
            GameObject lumen = EnsureLumen();
            EnsureCamera(lumen.transform);
            EnsureExitTrigger();
            EnsureHud();
            EnsureIntroSequence();
            EnsureNovaNarrationUI();
            EnsureQuizUI();
            EnsureSkybox();
            EnsureBackgroundDecor();
            EnsureAudioManager();
            EnsureAllPlanetRoute();
            EnsureMainMenu();
            EnsurePauseMenu();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[LUMEN] Cena montada. Pressione Play para testar. " +
                      "Controles: W/S avançar, A/D girar, Espaço/Ctrl subir e descer, X frear, " +
                      "Shift boost, R resgate, Esc sair. " +
                      "Confira o Console para avisos de qualquer asset ou som faltando.");
        }

        private static void EnsureGameManager()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        /// <summary>
        /// Cena montada em runtime nao ganha EventSystem automaticamente. Sem
        /// EventSystem, NENHUM clique de mouse funciona nos botoes (menu, quiz).
        /// Precisa do Input Manager (Old) ou Both habilitado no Player Settings.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static GameObject EnsureLumen()
        {
            GameObject lumen = GameObject.Find("LUMEN");
            if (lumen == null)
            {
                lumen = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                lumen.name = "LUMEN";
                lumen.tag = "Player";

                // Substitui o collider padrao da capsula por um esferico, mais barato
                // e mais previsivel para deteccao de gatilho.
                Object.DestroyImmediate(lumen.GetComponent<Collider>());
                var sphereCollider = lumen.AddComponent<SphereCollider>();
                sphereCollider.radius = 0.7f;

                // Necessario para o Unity disparar OnTriggerEnter de forma confiavel.
                var rb = lumen.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Reposiciona sempre: a nave comeca longe do planeta enorme.
            lumen.transform.position = LumenStartPosition;
            lumen.transform.rotation = Quaternion.identity; // comeca olhando para Netuno (+Z)

            // Tag "Player" SEMPRE garantida: o farol do tutorial e o gatilho do
            // planeta detectam o LUMEN por ela.
            if (lumen.tag != "Player")
                lumen.tag = "Player";

            if (lumen.GetComponent<LumenEnergySystem>() == null)
                lumen.AddComponent<LumenEnergySystem>();
            if (lumen.GetComponent<LumenController>() == null)
                lumen.AddComponent<LumenController>();

            var lumenController = lumen.GetComponent<LumenController>();
            var energy = lumen.GetComponent<LumenEnergySystem>();

            if (lumenController != null)
            {
                lumenController.SetOpenWorldRadius(GameTuning.OpenWorldRadius);
                lumenController.ApplyMovementTuning(GameTuning.Acceleration, GameTuning.MaxSpeed,
                    GameTuning.BoostSpeed, GameTuning.CoastDamping, GameTuning.BrakeDamping,
                    GameTuning.RotationSmooth);
            }

            if (energy != null)
                energy.Configure(GameTuning.MaxEnergy, GameTuning.ThrustDrainPerSecond,
                    GameTuning.IdleDrainPerSecond);

            return lumen;
        }

        private static void EnsureCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGO = new GameObject("Main Camera");
                camGO.tag = "MainCamera";
                cam = camGO.AddComponent<Camera>();
                camGO.AddComponent<AudioListener>();
            }

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.SetTarget(target);

            // Plano distante generoso: cobre a rota inteira (chega a ~4.550) com folga.
            cam.farClipPlane = 20000f;
        }

        private static void EnsureExitTrigger()
        {
            var exit = GameObject.Find("TutorialExit");
            if (exit == null)
            {
                exit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                exit.name = "TutorialExit";
                exit.GetComponent<Renderer>().enabled = false;
            }

            exit.transform.position = TutorialExitPosition;
            exit.transform.localScale = Vector3.one * 40f;

            var col = exit.GetComponent<Collider>();
            if (col == null) col = exit.AddComponent<SphereCollider>();
            col.isTrigger = true;

            if (exit.GetComponent<TutorialExitTrigger>() == null)
                exit.AddComponent<TutorialExitTrigger>();

            EnsureExitBeacon(exit.transform);
        }

        /// <summary>
        /// Farol visivel (esfera brilhante do pacote) marcando a saida do tutorial.
        /// Ao atravessa-lo, o tutorial termina e o farol desaparece.
        /// </summary>
        private static void EnsureExitBeacon(Transform exitRoot)
        {
            var beacon = exitRoot.Find("TutorialBeacon");
            if (beacon != null)
            {
                beacon.localPosition = Vector3.zero;
                beacon.localScale = Vector3.one * 30f;
                return;
            }

            string beaconPath = $"{PlanetsPackPath}/Prefabs/Sun Sphere.prefab";
            var beaconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(beaconPath);
            if (beaconPrefab == null)
            {
                Debug.LogWarning($"[LUMEN] Não encontrei '{beaconPath}' para o farol de saída do tutorial.");
                return;
            }

            var beaconObj = PrefabUtility.InstantiatePrefab(beaconPrefab) as GameObject;
            if (beaconObj == null) return;

            beaconObj.name = "TutorialBeacon";
            beaconObj.transform.SetParent(exitRoot, false);
            beaconObj.transform.localPosition = Vector3.zero;
            beaconObj.transform.localScale = Vector3.one * 30f;

            foreach (var trigCol in beaconObj.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(trigCol);
        }

        private static void EnsureHud()
        {
            GameObject canvasGO = GameObject.Find("HUD_Canvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("HUD_Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            var hud = canvasGO.GetComponent<HUDController>();
            if (hud == null) hud = canvasGO.AddComponent<HUDController>();

            // Duas barras empilhadas no canto superior esquerdo:
            // SINAL (Terra) em cima e COMBUSTIVEL em baixo.
            var signalBar = EnsureHudBar(canvasGO.transform, "SignalBar",
                new Color(0.2f, 0.9f, 1f), new Vector2(0.02f, 0.90f), new Vector2(0.30f, 0.94f));
            var energyBar = EnsureHudBar(canvasGO.transform, "EnergyBar",
                new Color(1f, 0.75f, 0.2f), new Vector2(0.02f, 0.84f), new Vector2(0.30f, 0.88f));

            var signalLabel = FindOrCreate(canvasGO.transform, "SignalLabel", CreateSignalLabel);
            var energyLabel = FindOrCreate(canvasGO.transform, "EnergyLabel", CreateEnergyLabel);

            PositionAt(signalLabel.transform, new Vector2(0.02f, 0.943f), new Vector2(0.30f, 0.975f));
            PositionAt(energyLabel.transform, new Vector2(0.02f, 0.883f), new Vector2(0.30f, 0.915f));

            var objective = FindOrCreate(canvasGO.transform, "ObjectiveLabel", CreateObjectiveLabel);
            var fragments = FindOrCreate(canvasGO.transform, "FragmentsLabel", CreateFragmentsLabel);
            var navArrow = FindOrCreate(canvasGO.transform, "NavArrow", CreateNavArrow);

            // Porcentagem vive DENTRO de cada barra (PercentLabel criado junto com elas).
            var signalPercent = signalBar.transform.Find("PercentLabel").GetComponent<Text>();
            var energyPercent = energyBar.transform.Find("PercentLabel").GetComponent<Text>();

            hud.Configure(energyBar, signalBar, objective, fragments, signalLabel, energyLabel,
                          energyPercent, signalPercent);
            hud.SetupNavigation(navArrow);
        }

        /// <summary>Cria uma barra "vazada" (moldura + fundo + preenchimento + % dentro),
        /// ou conserta uma antiga sem visual novo (sem Border/PercentLabel).</summary>
        private static Slider EnsureHudBar(Transform parent, string name, Color fill, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = parent.Find(name);
            if (go != null)
            {
                var slider = go.GetComponent<Slider>();
                var bg = go.Find("Background");
                var bgImg = bg != null ? bg.GetComponent<Image>() : null;

                // Reutiliza apenas se for EXATAMENTE o estilo vazado atual.
                bool hollowStyle = slider != null && slider.fillRect != null &&
                                   go.Find("Fill") != null && go.Find("Border") != null &&
                                   go.Find("PercentLabel") != null &&
                                   bgImg != null && Mathf.Abs(bgImg.color.a - 0.15f) <= 0.001f;

                if (hollowStyle)
                {
                    PositionAt(go, anchorMin, anchorMax);
                    return slider;
                }

                Object.DestroyImmediate(go.gameObject); // estilo antigo: recria no padrao vazado
            }

            var bar = CreateHudBar(parent, name, fill);
            PositionAt(bar.transform, anchorMin, anchorMax);
            return bar;
        }

        private static Slider CreateHudBar(Transform parent, string name, Color fillColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            // Moldura "vazada": borda colorida grossa ao redor...
            var border = new GameObject("Border", typeof(RectTransform));
            border.transform.SetParent(go.transform, false);
            RectStretch(border.GetComponent<RectTransform>());
            var borderImg = border.AddComponent<Image>();
            borderImg.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0.9f);
            borderImg.raycastTarget = false;

            // ...miolo TRANSPARENTE (so um tinte levinho para destacar do cenario).
            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            RectStretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<RectTransform>().offsetMin = new Vector2(5f, 5f);
            bg.GetComponent<RectTransform>().offsetMax = new Vector2(-5f, -5f);
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.15f);
            bgImg.raycastTarget = false;

            // ...preenchimento que cresce conforme combustivel/sinal.
            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, false);
            RectStretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<RectTransform>().offsetMin = new Vector2(5f, 5f);
            fill.GetComponent<RectTransform>().offsetMax = new Vector2(-5f, -5f);
            var fillImg = fill.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.color = fillColor;
            fillImg.raycastTarget = false;

            // Porcentagem DENTRO da barra (por cima do preenchimento).
            var percent = new GameObject("PercentLabel", typeof(RectTransform));
            percent.transform.SetParent(go.transform, false);
            RectStretch(percent.GetComponent<RectTransform>());
            var percentText = percent.AddComponent<Text>();
            percentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            percentText.fontSize = 15;
            percentText.fontStyle = FontStyle.Bold;
            percentText.alignment = TextAnchor.MiddleCenter;
            percentText.color = Color.white;
            percentText.raycastTarget = false;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = borderImg;
            slider.minValue = 0f;
            slider.maxValue = 100f;

            return slider;
        }

        private static Text CreateSignalLabel(Transform parent)
        {
            return CreateBarLabel(parent, "SignalLabel", "SINAL", new Color(0.3f, 0.9f, 1f));
        }

        private static Text CreateEnergyLabel(Transform parent)
        {
            return CreateBarLabel(parent, "EnergyLabel", "COMBUSTÍVEL", new Color(1f, 0.8f, 0.3f));
        }

        private static Text CreateBarLabel(Transform parent, string name, string content, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 13;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.LowerLeft;
            text.text = content;

            return text;
        }

        private static void PositionAt(Transform t, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rt = t as RectTransform;
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void RectStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static T FindOrCreate<T>(Transform parent, string childName, System.Func<Transform, T> factory) where T : Component
        {
            var existing = parent.Find(childName);
            if (existing != null) return existing.GetComponent<T>();
            return factory(parent);
        }

        private static Text CreateObjectiveLabel(Transform parent)
        {
            var go = new GameObject("ObjectiveLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.LowerLeft;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.02f);
            rt.anchorMax = new Vector2(0.85f, 0.11f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return text;
        }

        private static Text CreateFragmentsLabel(Transform parent)
        {
            var go = new GameObject("FragmentsLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.UpperRight;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.68f, 0.92f);
            rt.anchorMax = new Vector2(0.98f, 0.98f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return text;
        }

        private static Text CreateNavArrow(Transform parent)
        {
            var go = new GameObject("NavArrow", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(64f, 64f);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 44;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.4f, 0.9f, 1f, 0.9f);
            text.text = "\u25B2"; // triangulo apontando para cima
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        private static void EnsureIntroSequence()
        {
            if (Object.FindFirstObjectByType<IntroSequenceController>() != null) return;

            var canvasGO = new GameObject("Intro_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // fica por cima de tudo
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            SetFullScreen(panel.GetComponent<RectTransform>());
            var bg = panel.AddComponent<Image>();
            bg.color = Color.black;

            var text = CreateLabel(panel.transform, "LogText", 28, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f));

            var intro = canvasGO.AddComponent<IntroSequenceController>();
            intro.Configure(panel, text);
        }

        private static void EnsureNovaNarrationUI()
        {
            if (Object.FindFirstObjectByType<NovaNarrationController>() != null) return;

            var canvasGO = new GameObject("Nova_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5; // acima do HUD, abaixo do quiz e da intro
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.1f, 0.06f);
            panelRt.anchorMax = new Vector2(0.9f, 0.2f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            var text = CreateLabel(panel.transform, "Caption", 20, TextAnchor.MiddleCenter,
                Vector2.zero, Vector2.one, new Vector2(16, 8), new Vector2(-16, -8));
            text.color = new Color(0.6f, 0.9f, 1f);

            panel.SetActive(false);

            var narrator = canvasGO.AddComponent<NovaNarrationController>();
            narrator.Configure(panel, text);
        }

        private static void EnsureQuizUI()
        {
            if (Object.FindFirstObjectByType<QuizChallengeController>() != null) return;

            var canvasGO = new GameObject("Quiz_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.2f, 0.15f);
            panelRt.anchorMax = new Vector2(0.8f, 0.85f);
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            var questionText = CreateLabel(panel.transform, "Question", 22, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.68f), new Vector2(0.95f, 0.95f));

            var buttons = new Button[3];
            var labels = new Text[3];
            float[] tops = { 0.60f, 0.40f, 0.20f };

            for (int i = 0; i < 3; i++)
            {
                var btnGO = new GameObject($"Option{i}", typeof(RectTransform));
                btnGO.transform.SetParent(panel.transform, false);
                var btnRt = btnGO.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0.08f, tops[i] - 0.16f);
                btnRt.anchorMax = new Vector2(0.92f, tops[i]);
                btnRt.offsetMin = Vector2.zero;
                btnRt.offsetMax = Vector2.zero;

                var img = btnGO.AddComponent<Image>();
                img.color = new Color(0.2f, 0.4f, 0.6f, 1f);
                buttons[i] = btnGO.AddComponent<Button>();

                labels[i] = CreateLabel(btnGO.transform, "Label", 16, TextAnchor.MiddleCenter,
                    Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-10, -4));
            }

            var feedbackText = CreateLabel(panel.transform, "Feedback", 16, TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0f), new Vector2(0.95f, 0.14f));
            feedbackText.color = new Color(1f, 0.9f, 0.4f);

            panel.SetActive(false);

            var quiz = canvasGO.AddComponent<QuizChallengeController>();
            quiz.Configure(panel, questionText, feedbackText, buttons, labels);
        }

        /// <summary>Tela inicial: titulo "Órbita Perdida" + botao "Jogar".</summary>
        private static void EnsureMainMenu()
        {
            if (Object.FindFirstObjectByType<MainMenuController>() != null) return;

            var canvasGO = CreateOverlayCanvas("Menu_Canvas", 30);
            var panel = CreateFullscreenPanel(canvasGO.transform, new Color(0.02f, 0.03f, 0.08f, 0.85f));

            var title = CreateLabel(panel.transform, "Title", 64, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.8f));
            title.text = "Órbita Perdida";
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.6f, 0.9f, 1f);

            var play = CreateButton(panel.transform, "PlayButton", "Jogar",
                new Vector2(0.4f, 0.3f), new Vector2(0.6f, 0.42f));

            var menu = canvasGO.AddComponent<MainMenuController>();
            menu.Configure(panel, play);
        }

        /// <summary>Confirmacao de saida (Esc): "Deseja realmente sair do jogo?" Sim / Não.</summary>
        private static void EnsurePauseMenu()
        {
            if (Object.FindFirstObjectByType<PauseMenuController>() != null) return;

            var canvasGO = CreateOverlayCanvas("Pause_Canvas", 40);
            var panel = CreateFullscreenPanel(canvasGO.transform, new Color(0f, 0f, 0f, 0.75f));

            var message = CreateLabel(panel.transform, "Message", 36, TextAnchor.MiddleCenter,
                new Vector2(0.1f, 0.55f), new Vector2(0.9f, 0.75f));
            message.text = "Deseja realmente sair do jogo?";

            var yes = CreateButton(panel.transform, "YesButton", "Sim",
                new Vector2(0.28f, 0.35f), new Vector2(0.46f, 0.47f));
            var no = CreateButton(panel.transform, "NoButton", "Não",
                new Vector2(0.54f, 0.35f), new Vector2(0.72f, 0.47f));

            panel.SetActive(false);

            var pause = canvasGO.AddComponent<PauseMenuController>();
            pause.Configure(panel, yes, no);
        }

        private static GameObject CreateOverlayCanvas(string name, int sortingOrder)
        {
            var canvasGO = new GameObject(name);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            return canvasGO;
        }

        private static GameObject CreateFullscreenPanel(Transform parent, Color color)
        {
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            SetFullScreen(panel.GetComponent<RectTransform>());
            var img = panel.AddComponent<Image>();
            img.color = color; // raycastTarget ligado: bloqueia cliques no jogo por baixo
            return panel;
        }

        private static Button CreateButton(Transform parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.4f, 0.6f, 1f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;

            var text = CreateLabel(go.transform, "Label", 28, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            text.text = label;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;

            return button;
        }

        private static void EnsureSkybox()
        {
            string path = $"{PlanetsPackPath}/Materials/Skybox.mat";
            var skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (skyboxMat == null)
            {
                Debug.LogWarning($"[LUMEN] Não encontrei '{path}'. Confirme se o pacote " +
                                  "'Planets of the Solar System 3D' foi importado antes de rodar de novo.");
                return;
            }

            RenderSettings.skybox = skyboxMat;
            DynamicGI.UpdateEnvironment();
        }

        private static void EnsureBackgroundDecor()
        {
            if (GameObject.Find("BackgroundDecor") != null) return;

            var root = new GameObject("BackgroundDecor");

            // So decoracao distante e nao-interativa.
            InstantiateDecor("Sun.prefab", new Vector3(0f, 180f, 950f), root.transform, addRotation: false);
            InstantiateDecor("Nebula_00.prefab", new Vector3(-220f, 40f, 180f), root.transform, addRotation: false);
        }

        private static void InstantiateDecor(string prefabFileName, Vector3 position, Transform parent, bool addRotation)
        {
            string objName = prefabFileName.Replace(".prefab", "");
            if (parent.Find(objName) != null) return;

            string path = $"{PlanetsPackPath}/Prefabs/{prefabFileName}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[LUMEN] Não encontrei '{path}'. Pulei esse elemento de cenário.");
                return;
            }

            var instanceObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instanceObj == null) return;

            instanceObj.name = objName;
            instanceObj.transform.SetParent(parent, false);
            instanceObj.transform.position = position;

            foreach (var col in instanceObj.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            if (addRotation)
                instanceObj.AddComponent<SlowRotator>();
        }

        private static void EnsureAudioManager()
        {
            var manager = Object.FindFirstObjectByType<AudioManager>();
            if (manager == null)
            {
                var go = new GameObject("AudioManager");
                go.AddComponent<AudioSource>();
                go.AddComponent<AudioSource>();
                manager = go.AddComponent<AudioManager>();
            }

            AutoAssignMusic(manager);
            AutoAssignSfx(manager);
        }

        private static void AutoAssignMusic(AudioManager manager)
        {
            if (manager.HasMusicClip) return;

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[LUMEN] Nenhum AudioClip encontrado em Assets/_Project/Audio para a trilha.");
                return;
            }

            var clip = FindClipByKeywords(guids, "theme", "music", "trilha", "tema")
                       ?? AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));

            manager.SetMusicClip(clip);
            Debug.Log($"[LUMEN] Trilha atribuída automaticamente: {AssetDatabase.GetAssetPath(clip)}");
        }

        private static void AutoAssignSfx(AudioManager manager)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" });
            if (guids.Length == 0) return;

            TryAssignSfx(manager.HasSelectSfx, manager.SetSelectSfx, guids, "Seleção",
                "select", "click", "hover", "beep", "seleciona");
            TryAssignSfx(manager.HasCorrectSfx, manager.SetCorrectSfx, guids, "Acerto",
                "correct", "success", "confirm", "acerto", "certo");
            TryAssignSfx(manager.HasIncorrectSfx, manager.SetIncorrectSfx, guids, "Erro",
                "incorrect", "error", "wrong", "fail", "erro");
            TryAssignSfx(manager.HasCollectSfx, manager.SetCollectSfx, guids, "Coleta",
                "collect", "pickup", "coin", "reward", "coleta", "fragment");
        }

        private static void TryAssignSfx(bool alreadyAssigned, System.Action<AudioClip> setter,
            string[] guids, string label, params string[] keywords)
        {
            if (alreadyAssigned) return;

            var clip = FindClipByKeywords(guids, keywords);
            if (clip == null)
            {
                Debug.LogWarning($"[LUMEN] Não achei um som de '{label}' automaticamente. " +
                                  "Arraste manualmente no AudioManager, ou renomeie o arquivo para conter '" + keywords[0] + "'.");
                return;
            }

            setter(clip);
            Debug.Log($"[LUMEN] Som de {label} atribuído automaticamente: {AssetDatabase.GetAssetPath(clip)}");
        }

        private static AudioClip FindClipByKeywords(string[] guids, params string[] keywords)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();

                foreach (string keyword in keywords)
                {
                    if (fileName.Contains(keyword.ToLowerInvariant()))
                        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }

        private static void EnsureAllPlanetRoute()
        {
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder(DataFolder, "Quizzes");
            EnsureFolder(DataFolder, "Phases");

            // 1) Dados (quiz + fase) de cada planeta da rota.
            var quizNetuno = EnsureQuizAsset("Netuno_Quiz.asset",
                "Netuno tem os ventos mais fortes do Sistema Solar, apesar de estar tão longe do Sol. " +
                "Qual é a velocidade aproximada desses ventos?",
                new[] { "Cerca de 2.100 km/h", "Cerca de 100 km/h", "Cerca de 500 km/h" }, 0,
                "Isso mesmo! Os ventos de Netuno chegam a 2.100 km/h — os mais rápidos já registrados em qualquer planeta.",
                "Não é bem isso. Pense em algo bem mais extremo — os mais rápidos do Sistema Solar.");

            var quizUrano = EnsureQuizAsset("Urano_Quiz.asset",
                "Urano é o único planeta que gira bem 'deitado', com o eixo quase no plano da órbita. " +
                "Qual é o efeito disso no planeta?",
                new[] { "Estações que duram cerca de 21 anos cada", "Ele não tem estações", "Ele gira super rápido" }, 0,
                "Isso mesmo! Cada polo de Urano passa mais de 20 anos de Sol e mais de 20 anos de escuridão por vez.",
                "Não — o eixo inclinado de Urano cria estações absurdamente longas, de mais de 20 anos.");

            var quizSaturno = EnsureQuizAsset("Saturno_Quiz.asset",
                "O que forma os anéis de Saturno?",
                new[] { "Milhões de fragmentos de gelo e rocha", "Gás comprimido pelo vento", "Poeira de meteoros em queda" }, 0,
                "Exato! Os anéis são formados por bilhões de fragmentos de gelo e rocha orbitando o planeta.",
                "Os anéis não são sólidos nem gasosos — pense em algo bem fragmentado rodando em órbita.");

            // O nome do ARQUIVO continua "Jupiter" (o path e o .meta nao mudam);
            // so o nome exibido em tela e acentuado.
            var quizJupiter = EnsureQuizAsset("Jupiter_Quiz.asset",
                "Que fenômeno aparece na superfície de Júpiter há séculos?",
                new[] { "A Grande Mancha Vermelha, uma tempestade maior que a Terra",
                        "Um vulcão gigante",
                        "Um oceano de lava" }, 0,
                "Correto! A Grande Mancha Vermelha é uma tempestade colossal observada há mais de 300 anos, maior que a Terra.",
                "Quase — repare na mancha enorme que gira no hemisfério sul do gigante gasoso.");

            var quizMarte = EnsureQuizAsset("Marte_Quiz.asset",
                "Por que Marte tem uma cor avermelhada?",
                new[] { "Óxido de ferro (ferrugem) na superfície", "Pedras vulcânicas quentes", "Gelo refletindo o céu" }, 0,
                "Perfeito! O solo de Marte é rico em óxido de ferro, que dá ao planeta o tom ferrugem.",
                "Não é calor — é a composição química do solo que dá essa cor a Marte.");

            var quizTerra = EnsureQuizAsset("Terra_Quiz.asset",
                "O que torna a Terra única no Sistema Solar, até hoje?",
                new[] { "Água líquida abundante e vida", "Tamanho recorde", "Maior número de luas" }, 0,
                "Exatamente! É o único mundo conhecido com água líquida em abundância e vida.",
                "Pense no que nenhum outro planeta conhecido tem de tão especial...");

            var phaseNetuno = EnsurePlanetPhaseData("Netuno_Phase.asset", "Netuno", quizNetuno,
                new[]
                {
                    "Aproximando de Netuno.",
                    "Netuno é o planeta mais distante do Sol, com temperaturas perto de -220 graus Celsius.",
                    "Mas não deixe o frio enganar: os ventos aqui são os mais violentos de todo o Sistema Solar.",
                }, 75f);

            var phaseUrano = EnsurePlanetPhaseData("Urano_Phase.asset", "Urano", quizUrano,
                new[]
                {
                    "Aproximando de Urano.",
                    "Urano gira de lado, como se rolasse por sua órbita.",
                    "Com isso, os polos passam décadas expostos ao Sol e depois à escuridão.",
                }, 75f);

            var phaseSaturno = EnsurePlanetPhaseData("Saturno_Phase.asset", "Saturno", quizSaturno,
                new[]
                {
                    "Aproximando de Saturno.",
                    "O gigante dos anéis: bilhões de fragmentos de gelo e rocha.",
                    "Você está passando pelo planeta mais fotogênico do Sistema Solar.",
                }, 75f);

            var phaseJupiter = EnsurePlanetPhaseData("Jupiter_Phase.asset", "Júpiter", quizJupiter,
                new[]
                {
                    "Aproximando de Júpiter.",
                    "O maior planeta de todos — sua Grande Mancha Vermelha é uma tempestade maior que a Terra.",
                    "Não há superfície sólida: é um gigante gasoso.",
                }, 75f);

            var phaseMarte = EnsurePlanetPhaseData("Marte_Phase.asset", "Marte", quizMarte,
                new[]
                {
                    "Aproximando de Marte.",
                    "O planeta vermelho, coberto por óxido de ferro.",
                    "Foi aqui que rovers já exploraram a superfície.",
                }, 75f);

            // Destino final: nao da fragmento (nextPlanet null encerra a missao).
            var phaseTerra = EnsurePlanetPhaseData("Terra_Phase.asset", "Terra", quizTerra,
                new[]
                {
                    "Aproximando da Terra.",
                    "Depois de coletar conhecimento planeta a planeta, o retorno está quase completo.",
                    "LUMEN, prepare-se para concluir o último quiz e voltar para casa.",
                }, 0f);

            // 2) Planetas em rota ZIGUE-ZAGUE, agora bem mais compacta: o jogador
            //    e guiado planeta a planeta sem precisar cruzar trechos enormes.
            var netuno = EnsurePlanet("Neptune.prefab", "Netuno_Encounter", NetunoPosition, phaseNetuno, 0);
            var urano  = EnsurePlanet("Uranus.prefab",  "Urano_Encounter",  UranoPosition, phaseUrano, 1);
            var saturno = EnsurePlanet("Saturn.prefab", "Saturno_Encounter", SaturnoPosition, phaseSaturno, 2);
            var jupiter = EnsurePlanet("Jupiter.prefab", "Jupiter_Encounter", JupiterPosition, phaseJupiter, 3);
            var marte  = EnsurePlanet("Mars.prefab",   "Marte_Encounter",   MartePosition, phaseMarte, 4);
            var terra  = EnsurePlanet("Earth.prefab",  "Terra_Encounter",   TerraPosition, phaseTerra, 5);

            // 3) Encadeia a rota: ao terminar um quiz, a seta aponta para o proximo.
            PlanetApproachTrigger[] route = { netuno, urano, saturno, jupiter, marte, terra };
            for (int i = 0; i < route.Length - 1; i++)
            {
                if (route[i] == null || route[i + 1] == null) continue;
                route[i].SetNextPlanet(route[i + 1].transform);
            }
        }

        private static void EnsureFolder(string parent, string newFolderName)
        {
            string fullPath = $"{parent}/{newFolderName}";
            if (AssetDatabase.IsValidFolder(fullPath)) return;
            AssetDatabase.CreateFolder(parent, newFolderName);
        }

        private static QuizQuestionData EnsureQuizAsset(string fileName, string question, string[] options,
            int correctIndex, string correctFeedback, string incorrectFeedback)
        {
            string path = $"{DataFolder}/Quizzes/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<QuizQuestionData>(path);
            if (existing != null) return existing;

            var quiz = ScriptableObject.CreateInstance<QuizQuestionData>();
            quiz.Question = question;
            quiz.Options = options;
            quiz.CorrectIndex = correctIndex;
            quiz.CorrectFeedback = correctFeedback;
            quiz.IncorrectFeedback = incorrectFeedback;

            AssetDatabase.CreateAsset(quiz, path);
            return quiz;
        }

        private static PhaseData EnsurePlanetPhaseData(string fileName, string planetName, QuizQuestionData quiz,
            string[] narration, float reward)
        {
            string path = $"{DataFolder}/Phases/{fileName}";
            var existing = AssetDatabase.LoadAssetAtPath<PhaseData>(path);
            if (existing != null) return existing;

            var phase = ScriptableObject.CreateInstance<PhaseData>();
            phase.PlanetName = planetName;
            phase.NarrationLines = narration;
            phase.Quiz = quiz;
            phase.FragmentEnergyReward = reward;

            AssetDatabase.CreateAsset(phase, path);
            return phase;
        }

        private static PlanetApproachTrigger EnsurePlanet(string prefabFileName, string objectName, Vector3 position,
            PhaseData phase, int routeIndex)
        {
            var existing = GameObject.Find(objectName);
            if (existing != null)
            {
                // Cena montada antes desta versao: reposiciona e reajusta o tamanho,
                // e liga/atualiza o trigger da rota se ainda nao existir.
                existing.transform.position = position;
                ApplyPlanetScale(existing, prefabFileName);
                NormalizeEncounterTrigger(existing);

                var trig = existing.GetComponent<PlanetApproachTrigger>();
                if (trig == null)
                {
                    if (existing.GetComponent<Collider>() == null)
                    {
                        var col = existing.AddComponent<SphereCollider>();
                        col.isTrigger = true;
                    }
                    trig = existing.AddComponent<PlanetApproachTrigger>();
                }

                trig.SetPhase(phase);
                trig.SetRouteIndex(routeIndex);
                return trig;
            }

            string prefabPath = $"{PlanetsPackPath}/Prefabs/{prefabFileName}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[LUMEN] Não encontrei '{prefabPath}'. A fase de {objectName} não foi montada — " +
                                  "confirme se o pacote de planetas está importado e rode o menu de novo.");
                return null;
            }

            var instanceObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instanceObj == null) return null;

            instanceObj.name = objectName;
            instanceObj.transform.position = position;

            ApplyPlanetScale(instanceObj, prefabFileName);
            instanceObj.AddComponent<SlowRotator>();

            foreach (var col in instanceObj.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);

            var sphereCollider = instanceObj.AddComponent<SphereCollider>();
            sphereCollider.isTrigger = true;
            // O collider cresce junto com a escala do objeto: compensa para que
            // a zona de encontro no MUNDO real fique proporcional ao planeta.
            float worldRadius = Mathf.Max(MinEncounterRadius, instanceObj.transform.localScale.x * EncounterFactor);
            sphereCollider.radius = worldRadius / instanceObj.transform.localScale.x;

            var trigger = instanceObj.AddComponent<PlanetApproachTrigger>();
            trigger.SetPhase(phase);
            trigger.SetRouteIndex(routeIndex);
            return trigger;
        }

        /// <summary>Aplica a escala proporcional ao planeta, se houver na tabela.</summary>
        private static void ApplyPlanetScale(GameObject instance, string prefabFileName)
        {
            if (PlanetScaleByPrefab.TryGetValue(prefabFileName, out float scale))
                instance.transform.localScale = Vector3.one * scale;
        }

        /// <summary>Compensa o trigger de encontro pela escala do planeta, mantendo o raio mundial proporcional.</summary>
        private static void NormalizeEncounterTrigger(GameObject planetObject)
        {
            var col = planetObject.GetComponent<SphereCollider>();
            if (col == null) return;

            float scale = planetObject.transform.localScale.x;
            if (scale <= 0.001f) scale = 1f;
            float worldRadius = Mathf.Max(MinEncounterRadius, scale * EncounterFactor);
            col.radius = worldRadius / scale;
        }

        // --- Utilitarios de UI ---

        private static void SetFullScreen(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text CreateLabel(Transform parent, string name, int fontSize, TextAnchor alignment,
            Vector2 anchorMin, Vector2 anchorMax, Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = alignment;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin ?? Vector2.zero;
            rt.offsetMax = offsetMax ?? Vector2.zero;

            return text;
        }
    }
}
#endif
