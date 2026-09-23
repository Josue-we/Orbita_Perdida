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
    /// LUMEN, camera, HUD, intro, skybox, cenario decorativo, audio, narracao da
    /// NOVA, quiz, e agora a Fase 2 (Netuno). Use o menu "LUMEN > Montar Cena de
    /// Tutorial" - e seguro rodar mais de uma vez, cada parte confere se ja
    /// existe antes de criar de novo.
    /// </summary>
    public static class TutorialSceneBuilder
    {
        private const string PlanetsPackPath = "Assets/Planets of the Solar System 3D";
        private const string DataFolder = "Assets/_Project/Data";

        // Todos os prefabs de planeta do pacote guardam escala 1 usando a mesma
        // malha esferica, entao por padrao todos apareceriam do mesmo tamanho.
        // Aqui aplicamos escala proporcional ao raio REAL de cada planeta, com
        // Netuno ancorado em 180 (= 6x do valor anterior de 30). O layout do
        // mundo inteiro e derivado desse valor.
        // Formula: scale = 180 * raioRealKm / 24622 (raio de Netuno).
        // Raio da zona de encontro: proporcional ao tamanho do planeta, com
        // minimo para nao disparar longe demais de planetas pequenos.
        private const float EncounterFactor = 1.7f;
        private const float MinEncounterRadius = 25f;

        // Distancias do mundo derivadas do tamanho de Netuno (escala 180 => raio ~180).
        // Nave em (0,0,40), portao/saida do tutorial em (0,0,120) e Netuno em
        // (0,10,560). A zona de encontro do quiz tem raio mundial ~306, entao ela
        // so comeca a valer em z ~254 - depois do portao.
        private static readonly Vector3 LumenStartPosition = new Vector3(0f, 0f, 40f);
        private static readonly Vector3 TutorialExitPosition = new Vector3(0f, 0f, 120f);
        private static readonly Vector3 NetunoPosition = new Vector3(0f, 10f, 560f);

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

            Debug.Log("[LUMEN] Cena montada. Pressione Play para testar. " +
                      "Controles: WASD para mover, Space/Ctrl para subir e descer. " +
                      "Confira o Console para avisos de qualquer asset ou som faltando.");
        }

        private static void EnsureGameManager()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        /// <summary>
        /// Cena montada em runtime nao ganha EventSystem automaticamente (o Unity
        /// so cria via menu de UI no editor). Sem EventSystem, NEHUM clique de
        /// mouse funciona nos botoes do quiz. Precisa do Input Manager (Old) ou
        /// Both habilitado no Player Settings, igual ao controle do LUMEN.
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

                // Necessario para o Unity disparar OnTriggerEnter de forma confiavel:
                // sem NENHUM Rigidbody entre os dois colliders envolvidos, triggers
                // entre objetos movidos so por Transform nao sao detectados sempre.
                var rb = lumen.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // Reposiciona sempre (mesmo se ja existir de cena antiga): a nave
            // comeca longe do planeta enorme (Netuno a 560 com raio ~180), com
            // bastante espaco para a viagem do tutorial ate o farol.
            lumen.transform.position = LumenStartPosition;

            // Tag "Player" SEMPRE garantida: o farol do tutorial e o gatilho do
            // planeta detectam o LUMEN por ela. Nave criada em versoes antigas
            // (ou manualmente) nao tinha a tag -> triggers nunca disparavam.
            if (lumen.tag != "Player")
                lumen.tag = "Player";

            if (lumen.GetComponent<LumenEnergySystem>() == null)
                lumen.AddComponent<LumenEnergySystem>();
            if (lumen.GetComponent<LumenController>() == null)
                lumen.AddComponent<LumenController>();

            // O mundo cresceu junto com a rota ate a Terra (3.600+): garante que o limite
            // de voo acumule o raio aberto novo mesmo se a instancia guardar o valor antigo.
            var lumenController = lumen.GetComponent<LumenController>();
            if (lumenController != null) lumenController.SetOpenWorldRadius(4500f);

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

            // A rota chega a 3.600+ unidades; o plano distante padrão (1000) cortaria os planetas longos.
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

            // Zona ampla entre a nave (z = 40) e Netuno (z = 560): fica 80 m a
            // frente da nave, bem antes da zona de encontro do quiz (z ~254).
            // Voar para a frente guiado pelo farol atravessa a zona e encerra o tutorial.
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
        /// Ao atravessa-lo, o tutorial termina e o farol desaparece. Se o prefab
        /// nao existir, cai num anel invisivel - mas o HUD ainda guia o jogador.
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
                Debug.LogWarning($"[LUMEN] Nao encontrei '{beaconPath}' para o farol de saida do tutorial.");
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
            // SINAL (Terra) em cima e COMBUSTIVEL em baixo. Barras antigas sem
            // visual (Sliders nus) sao substituidas automaticamente.
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

                // Reutiliza apenas se for EXATAMENTE o estilo vazado atual
                // (miolo com alpha 0.15). Qualquer variacao antiga - mesmo que ja
                // tenha Border/PercentLabel - e recriada para nao vazar visual velho.
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

            // ...miolo TRANSPARENTE (so um tinte levinho para destacar do cenario):
            // sem preenchimento, a barra parece mesmo vazia.
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
            return CreateBarLabel(parent, "EnergyLabel", "COMBUSTIVEL", new Color(1f, 0.8f, 0.3f));
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

        private static void EnsureSkybox()
        {
            string path = $"{PlanetsPackPath}/Materials/Skybox.mat";
            var skyboxMat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (skyboxMat == null)
            {
                Debug.LogWarning($"[LUMEN] Nao encontrei '{path}'. Confirme se o pacote " +
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

            // So decoracao distante e nao-interativa (os planetas "de verdade",
            // alcancaveis, sao criados separadamente em EnsureAllPlanetRoute).
            // O Sol e empurrado para longe: Netuno gigante ocupava a posicao antiga.
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
                Debug.LogWarning($"[LUMEN] Nao encontrei '{path}'. Pulei esse elemento de cenario.");
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
            Debug.Log($"[LUMEN] Trilha atribuida automaticamente: {AssetDatabase.GetAssetPath(clip)}");
        }

        private static void AutoAssignSfx(AudioManager manager)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/_Project/Audio" });
            if (guids.Length == 0) return;

            TryAssignSfx(manager.HasSelectSfx, manager.SetSelectSfx, guids, "Selecao",
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
                Debug.LogWarning($"[LUMEN] Nao achei um som de '{label}' automaticamente. " +
                                  "Arraste manualmente no AudioManager, ou renomeie o arquivo para conter '" + keywords[0] + "'.");
                return;
            }

            setter(clip);
            Debug.Log($"[LUMEN] Som de {label} atribuido automaticamente: {AssetDatabase.GetAssetPath(clip)}");
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
                "Netuno tem os ventos mais fortes do Sistema Solar, apesar de estar tao longe do Sol. " +
                "Qual e a velocidade aproximada desses ventos?",
                new[] { "Cerca de 2.100 km/h", "Cerca de 100 km/h", "Cerca de 500 km/h" }, 0,
                "Isso mesmo! Os ventos de Netuno chegam a ate 2.100 km/h - os mais rapidos ja registrados em qualquer planeta.",
                "Nao e bem isso. Pense em algo bem mais extremo - os mais rapidos do Sistema Solar.");

            var quizUrano = EnsureQuizAsset("Urano_Quiz.asset",
                "Urano e o unico planeta que gira bem 'deitado', com o eixo quase no plano da orbita. " +
                "Qual e o efeito disso no planeta?",
                new[] { "Estacoes que duram cerca de 21 anos cada", "Ele nao tem estacoes", "Ele gira super rapido" }, 0,
                "Isso mesmo! Cada polo de Urano passa mais de 20 anos de Sol e mais de 20 anos de escuridao por vez.",
                "Nao - o eixo inclinado de Urano cria estacoes absurdamente longas, de mais de 20 anos.");

            var quizSaturno = EnsureQuizAsset("Saturno_Quiz.asset",
                "O que forma os aneis de Saturno?",
                new[] { "Milhoes de fragmentos de gelo e rocha", "Gas comprimido pelo vento", "Poeira de meteoros em queda" }, 0,
                "Exato! Os aneis sao formados por bilhoes de fragmentos de gelo e rocha orbitando o planeta.",
                "Os aneis nao sao solidos nem gasosos - pense em algo bem fragmentado rodando em orbita.");

            var quizJupiter = EnsureQuizAsset("Jupiter_Quiz.asset",
                "Que fenomeno aparece na superficie de Jupiter ha seculos?",
                new[] { "A Grande Mancha Vermelha, uma tempestade maior que a Terra",
                        "Um vulcao gigante",
                        "Um oceano de lava" }, 0,
                "Correto! A Grande Mancha Vermelha e uma tempestade colossal observada ha mais de 300 anos, maior que a Terra.",
                "Quase - repare na mancha enorme que gira no hemisferio sul do gigante gasoso.");

            var quizMarte = EnsureQuizAsset("Marte_Quiz.asset",
                "Por que Marte tem uma cor avermelhada?",
                new[] { "Oxido de ferro (ferrugem) na superficie", "Pedras vulcanicas quentes", "Gelo refletindo o ceu" }, 0,
                "Perfeito! O solo de Marte e rico em oxido de ferro, que da ao planeta o tom ferrugem.",
                "Nao e calor - e a composicao quimica do solo que da essa cor a Marte.");

            var quizTerra = EnsureQuizAsset("Terra_Quiz.asset",
                "O que torna a Terra unica no Sistema Solar, ate hoje?",
                new[] { "Agua liquida abundante e vida", "Tamanho recorde", "Maior numero de luas" }, 0,
                "Exatamente! E o unico mundo conhecido com agua liquida em abundancia e vida.",
                "Pense no que nenhum outro planeta conhecido tem de tao especial...");

            var phaseNetuno = EnsurePlanetPhaseData("Netuno_Phase.asset", "Netuno", quizNetuno,
                new[]
                {
                    "Aproximando de Netuno.",
                    "Netuno e o planeta mais distante do Sol, com temperaturas perto de -220 graus Celsius.",
                    "Mas nao deixe o frio enganar: os ventos aqui sao os mais violentos de todo o Sistema Solar.",
                }, 30f);

            var phaseUrano = EnsurePlanetPhaseData("Urano_Phase.asset", "Urano", quizUrano,
                new[]
                {
                    "Aproximando de Urano.",
                    "Urano gira de lado, como se rolasse por sua orbita.",
                    "Com isso, os polos passam decadas expostos ao Sol e depois a escuridao.",
                }, 30f);

            var phaseSaturno = EnsurePlanetPhaseData("Saturno_Phase.asset", "Saturno", quizSaturno,
                new[]
                {
                    "Aproximando de Saturno.",
                    "O gigante dos aneis: bilhoes de fragmentos de gelo e rocha.",
                    "Voce esta passando pelo planeta mais fotogenico do Sistema Solar.",
                }, 30f);

            var phaseJupiter = EnsurePlanetPhaseData("Jupiter_Phase.asset", "Jupiter", quizJupiter,
                new[]
                {
                    "Aproximando de Jupiter.",
                    "O maior planeta de todos - sua Grande Mancha Vermelha e uma tempestade maior que a Terra.",
                    "Nao ha superficie solida: e um gigante gasoso.",
                }, 30f);

            var phaseMarte = EnsurePlanetPhaseData("Marte_Phase.asset", "Marte", quizMarte,
                new[]
                {
                    "Aproximando de Marte.",
                    "O planeta vermelho, coberto por oxido de ferro.",
                    "Foi aqui que rovers ja exploraram a superficie.",
                }, 30f);

            // Destino final: nao da fragmento (nextPlanet null encerra a missao).
            var phaseTerra = EnsurePlanetPhaseData("Terra_Phase.asset", "Terra", quizTerra,
                new[]
                {
                    "Aproximando da Terra.",
                    "Depois de coletar conhecimento planeta a planeta, o retorno esta quase completo.",
                    "LUMEN, prepare-se para concluir o ultimo quiz e voltar para casa.",
                }, 0f);

            // 2) Planetas em rota ZIGUE-ZAGUE (x: +1100 -> -1900 -> +600 -> -200),
            //    nada de linha reta ate a Terra: o jogador e guiado planeta a planeta.
            var netuno = EnsurePlanet("Neptune.prefab", "Netuno_Encounter", NetunoPosition, phaseNetuno, 0);
            var urano  = EnsurePlanet("Uranus.prefab",  "Urano_Encounter",  new Vector3(1100f, 30f, 1150f), phaseUrano, 1);
            var saturno = EnsurePlanet("Saturn.prefab", "Saturno_Encounter", new Vector3(0f, 50f, 1750f), phaseSaturno, 2);
            var jupiter = EnsurePlanet("Jupiter.prefab", "Jupiter_Encounter", new Vector3(-1900f, 80f, 2350f), phaseJupiter, 3);
            var marte  = EnsurePlanet("Mars.prefab",   "Marte_Encounter",   new Vector3(600f, 40f, 3000f), phaseMarte, 4);
            var terra  = EnsurePlanet("Earth.prefab",  "Terra_Encounter",   new Vector3(-200f, 20f, 3600f), phaseTerra, 5);

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
                Debug.LogWarning($"[LUMEN] Nao encontrei '{prefabPath}'. A fase de {objectName} nao foi montada - " +
                                  "confirme se o pacote de planetas esta importado e rode o menu de novo.");
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
