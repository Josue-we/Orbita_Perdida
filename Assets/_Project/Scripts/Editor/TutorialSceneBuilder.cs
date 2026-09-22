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
            EnsureNetunoPhase();

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

            // O mundo cresceu junto com Netuno: garante que o limite de voo acumule
            // o raio aberto novo mesmo se a instancia guardar o valor antigo.
            var lumenController = lumen.GetComponent<LumenController>();
            if (lumenController != null) lumenController.SetOpenWorldRadius(900f);

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

            hud.Configure(energyBar, signalBar, objective, fragments, signalLabel, energyLabel);
            hud.SetupNavigation(navArrow);
        }

        /// <summary>Cria uma barra com fundo e preenchimento visiveis, ou conserta uma antiga sem visual.</summary>
        private static Slider EnsureHudBar(Transform parent, string name, Color fill, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = parent.Find(name);
            if (go != null)
            {
                var slider = go.GetComponent<Slider>();
                if (slider != null && slider.fillRect != null && go.Find("Fill") != null)
                {
                    PositionAt(go, anchorMin, anchorMax);
                    return slider;
                }

                Object.DestroyImmediate(go.gameObject); // barra antiga nua (sem imagens): recria
            }

            var bar = CreateHudBar(parent, name, fill);
            PositionAt(bar.transform, anchorMin, anchorMax);
            return bar;
        }

        private static Slider CreateHudBar(Transform parent, string name, Color fillColor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var bg = new GameObject("Background", typeof(RectTransform));
            bg.transform.SetParent(go.transform, false);
            RectStretch(bg.GetComponent<RectTransform>());
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.55f);

            var fill = new GameObject("Fill", typeof(RectTransform));
            fill.transform.SetParent(go.transform, false);
            RectStretch(fill.GetComponent<RectTransform>());
            var fillImg = fill.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.color = fillColor;

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = bgImg;
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

            // So decoracao distante e nao-interativa (o Netuno "de verdade",
            // alcancavel, e criado separadamente em EnsureNetunoPhase).
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

        private static void EnsureNetunoPhase()
        {
            EnsureFolder("Assets/_Project", "Data");
            EnsureFolder(DataFolder, "Quizzes");
            EnsureFolder(DataFolder, "Phases");

            var quiz = EnsureNetunoQuiz();
            var phase = EnsureNetunoPhaseData(quiz);
            EnsureNetunoPlanet(phase);
        }

        private static void EnsureFolder(string parent, string newFolderName)
        {
            string fullPath = $"{parent}/{newFolderName}";
            if (AssetDatabase.IsValidFolder(fullPath)) return;
            AssetDatabase.CreateFolder(parent, newFolderName);
        }

        private static QuizQuestionData EnsureNetunoQuiz()
        {
            string path = $"{DataFolder}/Quizzes/Netuno_Quiz.asset";
            var existing = AssetDatabase.LoadAssetAtPath<QuizQuestionData>(path);
            if (existing != null) return existing;

            var quiz = ScriptableObject.CreateInstance<QuizQuestionData>();
            quiz.Question = "Netuno tem os ventos mais fortes do Sistema Solar, apesar de estar tao longe do Sol. " +
                             "Qual e a velocidade aproximada desses ventos?";
            quiz.Options = new[]
            {
                "Cerca de 2.100 km/h",
                "Cerca de 100 km/h",
                "Cerca de 500 km/h",
            };
            quiz.CorrectIndex = 0;
            quiz.CorrectFeedback = "Isso mesmo! Os ventos de Netuno chegam a ate 2.100 km/h - os mais rapidos ja registrados em qualquer planeta.";
            quiz.IncorrectFeedback = "Nao e bem isso. Pense em algo bem mais extremo - os mais rapidos do Sistema Solar.";

            AssetDatabase.CreateAsset(quiz, path);
            return quiz;
        }

        private static PhaseData EnsureNetunoPhaseData(QuizQuestionData quiz)
        {
            string path = $"{DataFolder}/Phases/Netuno_Phase.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PhaseData>(path);
            if (existing != null) return existing;

            var phase = ScriptableObject.CreateInstance<PhaseData>();
            phase.PlanetName = "Netuno";
            phase.NarrationLines = new[]
            {
                "Aproximando de Netuno.",
                "Netuno e o planeta mais distante do Sol, com temperaturas perto de -220 graus Celsius.",
                "Mas nao deixe o frio enganar: os ventos aqui sao os mais violentos de todo o Sistema Solar.",
            };
            phase.Quiz = quiz;
            phase.FragmentEnergyReward = 30f;

            AssetDatabase.CreateAsset(phase, path);
            return phase;
        }

        private static void EnsureNetunoPlanet(PhaseData phase)
        {
            var existing = GameObject.Find("Netuno_Encounter");
            if (existing != null)
            {
                // Cena montada antes desta versao: reposiciona e reajusta o
                // tamanho proporcional. O trigger e o PhaseData ja existem.
                existing.transform.position = NetunoPosition;
                ApplyPlanetScale(existing, "Neptune.prefab");
                NormalizeEncounterTrigger(existing);
                return;
            }

            string prefabPath = $"{PlanetsPackPath}/Prefabs/Neptune.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[LUMEN] Nao encontrei '{prefabPath}'. A Fase 2 nao foi montada - " +
                                  "confirme se o pacote de planetas esta importado e rode o menu de novo.");
                return;
            }

            var instanceObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instanceObj == null) return;

            instanceObj.name = "Netuno_Encounter";
            instanceObj.transform.position = NetunoPosition; // longe: planeta gigante ocupa o horizonte

            ApplyPlanetScale(instanceObj, "Neptune.prefab");
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
