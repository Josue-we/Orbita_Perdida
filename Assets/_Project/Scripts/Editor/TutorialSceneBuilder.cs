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
            EnsureSkybox();
            EnsureBackgroundDecor();
            EnsureAudioManager();

            Debug.Log("[LUMEN] Cena de tutorial montada. Pressione Play para testar. " +
                      "Controles: WASD para mover, Space/Ctrl para subir e descer. " +
                      "Nao esqueca de arrastar a trilha sonora para o AudioManager no Inspector.");
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
            if (Object.FindFirstObjectByType<AudioManager>() != null) return;

            var go = new GameObject("AudioManager");
            go.AddComponent<AudioSource>();
            go.AddComponent<AudioManager>();
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
