#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Gameplay;
using Lumen.Cameras;
using Lumen.UI;
using Lumen.Audio;
using Lumen.Environment;
using Lumen.Narrative;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor que monta a cena de Tutorial automaticamente:
    /// GameManager, LUMEN (placeholder), camera, gatilho de saida, HUD,
    /// skybox estrelado, planetas decorativos de fundo e o gerenciador de
    /// audio. Use o menu "LUMEN > Montar Cena de Tutorial" com uma cena
    /// vazia (ou ja montada) aberta - e seguro rodar mais de uma vez, cada
    /// parte confere se ja existe antes de criar de novo.
    /// </summary>
    public static class TutorialSceneBuilder
    {
        private const string PlanetsPackPath = "Assets/Planets of the Solar System 3D";

        [MenuItem("LUMEN/Montar Cena de Tutorial")]
        public static void BuildTutorialScene()
        {
            EnsureGameManager();
            GameObject lumen = EnsureLumen();
            EnsureCamera(lumen.transform);
            EnsureExitTrigger();
            EnsureHud();
            EnsureIntroSequence();
            EnsureSkybox();
            EnsureBackgroundDecor();
            EnsureAudioManager();

            Debug.Log("[LUMEN] Cena de tutorial montada. Pressione Play para testar. " +
                      "Controles: WASD para mover, Space/Ctrl para subir e descer. " +
                      "Confira o AudioManager no Inspector: musica e efeitos sonoros sao " +
                      "atribuidos automaticamente se estiverem em Assets/_Project/Audio " +
                      "com nomes reconheciveis (veja o Console para avisos de qualquer som faltando).");
        }

        private static void EnsureGameManager()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;

            var go = new GameObject("GameManager");
            go.AddComponent<GameManager>();
        }

        private static GameObject EnsureLumen()
        {
            GameObject lumen = GameObject.Find("LUMEN");
            if (lumen != null) return lumen;

            lumen = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            lumen.name = "LUMEN";
            lumen.transform.position = Vector3.zero;
            lumen.tag = "Player";

            // Substitui o collider padrao da capsula por um esferico, mais barato
            // e mais previsivel para deteccao de gatilho.
            Object.DestroyImmediate(lumen.GetComponent<Collider>());
            var sphereCollider = lumen.AddComponent<SphereCollider>();
            sphereCollider.radius = 0.7f;

            lumen.AddComponent<LumenEnergySystem>();
            lumen.AddComponent<LumenController>();

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
            if (GameObject.Find("TutorialExit") != null) return;

            var exit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            exit.name = "TutorialExit";
            exit.transform.position = new Vector3(0f, 0f, 35f);
            exit.transform.localScale = Vector3.one * 4f;
            exit.GetComponent<Renderer>().enabled = false; // invisivel, so o trigger importa

            exit.AddComponent<TutorialExitTrigger>();
        }

        private static void EnsureHud()
        {
            if (Object.FindFirstObjectByType<HUDController>() != null) return;

            var canvasGO = new GameObject("HUD_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var slider = CreateEnergyBar(canvasGO.transform);
            var label = CreateObjectiveLabel(canvasGO.transform);

            var hud = canvasGO.AddComponent<HUDController>();
            hud.Configure(slider, label);
        }

        private static Slider CreateEnergyBar(Transform parent)
        {
            var go = new GameObject("EnergyBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.92f);
            rt.anchorMax = new Vector2(0.32f, 0.97f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go.AddComponent<Slider>();
        }

        private static void EnsureIntroSequence()
        {
            if (Object.FindFirstObjectByType<IntroSequenceController>() != null) return;

            var canvasGO = new GameObject("Intro_Canvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10; // fica por cima do HUD
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(canvasGO.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = Color.black;

            var textGO = new GameObject("LogText", typeof(RectTransform));
            textGO.transform.SetParent(panel.transform, false);
            var text = textGO.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 28;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            var textRt = textGO.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.1f, 0.4f);
            textRt.anchorMax = new Vector2(0.9f, 0.6f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var intro = canvasGO.AddComponent<IntroSequenceController>();
            intro.Configure(panel, text);
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

            // Posicoes bem fora do raio de voo do tutorial (40 unidades), so para
            // dar profundidade visual ao fundo. Ajuste posicao/escala no Inspector
            // se algo vier grande ou pequeno demais - nao da para calibrar isso
            // sem ver a cena renderizada.
            InstantiateDecor("Sun.prefab", new Vector3(0f, 60f, 450f), root.transform, addRotation: false);
            InstantiateDecor("Neptune.prefab", new Vector3(180f, 25f, 260f), root.transform, addRotation: true);
            InstantiateDecor("Nebula_00.prefab", new Vector3(-220f, 40f, 180f), root.transform, addRotation: false);
        }

        private static void InstantiateDecor(string prefabFileName, Vector3 position, Transform parent, bool addRotation)
        {
            if (parent.Find(prefabFileName.Replace(".prefab", "")) != null) return; // ja instanciado

            string path = $"{PlanetsPackPath}/Prefabs/{prefabFileName}";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[LUMEN] Nao encontrei '{path}'. Pulei esse elemento de cenario.");
                return;
            }

            var instanceObj = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instanceObj == null) return;

            instanceObj.name = prefabFileName.Replace(".prefab", "");
            instanceObj.transform.SetParent(parent, false);
            instanceObj.transform.position = position;

            // Remove colliders: sao so decoracao de fundo, LUMEN nao deve
            // colidir com eles nesta fase.
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
                Debug.LogWarning("[LUMEN] Nenhum AudioClip encontrado em Assets/_Project/Audio para a trilha. " +
                                  "Importe a musica nessa pasta e rode o menu de novo, ou arraste manualmente " +
                                  "no campo Background Music do AudioManager.");
                return;
            }

            var clip = FindClipByKeywords(guids, "theme", "music", "trilha", "tema");
            if (clip == null)
            {
                // Nenhum nome bateu com as palavras-chave: usa o primeiro AudioClip
                // encontrado, assumindo que so tem a trilha por enquanto.
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

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
            if (alreadyAssigned) return; // ja foi escolhido a mao, nao mexe

            var clip = FindClipByKeywords(guids, keywords);
            if (clip == null)
            {
                Debug.LogWarning($"[LUMEN] Nao achei um som de '{label}' automaticamente. " +
                                  "Arraste manualmente o clipe certo no AudioManager, ou renomeie o " +
                                  "arquivo para conter uma palavra como '" + keywords[0] + "'.");
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

        private static Text CreateObjectiveLabel(Transform parent)
        {
            var go = new GameObject("ObjectiveLabel", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAnchor.LowerLeft;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.02f, 0.02f);
            rt.anchorMax = new Vector2(0.7f, 0.08f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return text;
        }
    }
}
#endif
