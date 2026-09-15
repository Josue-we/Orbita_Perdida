#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Gameplay;
using Lumen.Cameras;
using Lumen.UI;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Ferramenta de Editor que monta a cena de Tutorial automaticamente:
    /// GameManager, LUMEN (placeholder), camera, gatilho de saida e HUD basico.
    /// Use o menu "LUMEN > Montar Cena de Tutorial" com uma cena vazia aberta.
    /// Isso evita ter que criar e configurar cada GameObject manualmente.
    /// </summary>
    public static class TutorialSceneBuilder
    {
        [MenuItem("LUMEN/Montar Cena de Tutorial")]
        public static void BuildTutorialScene()
        {
            EnsureGameManager();
            GameObject lumen = EnsureLumen();
            EnsureCamera(lumen.transform);
            EnsureExitTrigger();
            EnsureHud();

            Debug.Log("[LUMEN] Cena de tutorial montada. Pressione Play para testar. " +
                      "Controles: WASD para mover, Space/Ctrl para subir e descer.");
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
