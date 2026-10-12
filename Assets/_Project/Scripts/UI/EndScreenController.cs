using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Lumen.Core;

namespace Lumen.UI
{
    /// <summary>
    /// PROTOTIPO da tela final: aparece quando o LUMEN conclui o quiz da Terra (OnMissionComplete).
    /// Mostra "Missão cumprida", o sinal restabelecido, os fragmentos coletados com uma
    /// curiosidade de cada planeta e dois botoes: "Jogar novamente" e "Sair".
    /// A interface inteira e criada por codigo, entao funciona em qualquer cena.
    /// </summary>
    public class EndScreenController : MonoBehaviour
    {
        [SerializeField] private float delayBeforeShow = 2.2f;
        [SerializeField] private float fadeSeconds = 1.2f;

        private static readonly Dictionary<string, string> Facts = new Dictionary<string, string>
        {
            { "netuno",  "Ventos de até 2.100 km/h, os mais rápidos do Sistema Solar." },
            { "urano",   "Gira \"deitado\": seus polos passam décadas de Sol e de escuridão." },
            { "saturno", "Seus anéis são bilhões de fragmentos de gelo e rocha." },
            { "jupiter", "O maior planeta, com tempestades que duram séculos." },
            { "marte",   "O planeta vermelho guarda sinais de água do passado." },
        };

        private readonly List<string> _fragments = new List<string>();
        private GameObject _panelRoot;
        private CanvasGroup _group;
        private Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            if (Object.FindFirstObjectByType<EndScreenController>() != null) return;
            new GameObject("EndScreen").AddComponent<EndScreenController>();
        }

        private void Awake()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUi();
            _panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.OnFragmentCollected += HandleFragment;
            EventBus.OnMissionComplete += HandleMissionComplete;
        }

        private void OnDisable()
        {
            EventBus.OnFragmentCollected -= HandleFragment;
            EventBus.OnMissionComplete -= HandleMissionComplete;
        }

        private void HandleFragment(string planet)
        {
            if (!_fragments.Contains(planet)) _fragments.Add(planet);
        }

        private void HandleMissionComplete()
        {
            StopAllCoroutines();
            StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            // O PlanetApproachTrigger devolve o estado "Playing" logo apos o evento;
            // espera 1 quadro e trava a nave no estado final.
            yield return null;
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Ended);

            yield return new WaitForSecondsRealtime(delayBeforeShow);

            FillContent();
            _group.alpha = 0f;
            _panelRoot.SetActive(true);

            float t = 0f;
            while (t < fadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                _group.alpha = Mathf.Clamp01(t / fadeSeconds);
                yield return null;
            }
            _group.alpha = 1f;
        }

        // ------------------------------------------------------------------ UI

        private Text _summaryText;
        private Text _fragmentsText;

        private void BuildUi()
        {
            var canvasGO = new GameObject("EndScreen_Canvas");
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            UiTextTuning.ConfigureScaler(canvasGO.AddComponent<CanvasScaler>());
            canvasGO.AddComponent<GraphicRaycaster>();

            _panelRoot = new GameObject("Panel", typeof(RectTransform));
            _panelRoot.transform.SetParent(canvasGO.transform, false);
            Stretch(_panelRoot.GetComponent<RectTransform>());
            _panelRoot.AddComponent<Image>().color = new Color(0.01f, 0.02f, 0.06f, 0.94f);
            _group = _panelRoot.AddComponent<CanvasGroup>();

            var title = MakeLabel("Title", "MISSÃO CUMPRIDA", 76, FontStyle.Bold,
                new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.93f));
            title.color = new Color(0.6f, 0.93f, 1f);

            var subtitle = MakeLabel("Subtitle", "O LUMEN restabeleceu o contato com a Terra.", 38, FontStyle.Normal,
                new Vector2(0.1f, 0.69f), new Vector2(0.9f, 0.78f));
            subtitle.color = new Color(0.9f, 0.95f, 1f);

            _summaryText = MakeLabel("Summary", "", 34, FontStyle.Bold,
                new Vector2(0.1f, 0.60f), new Vector2(0.9f, 0.68f));
            _summaryText.color = new Color(1f, 0.82f, 0.35f);

            _fragmentsText = MakeLabel("Fragments", "", 30, FontStyle.Normal,
                new Vector2(0.14f, 0.24f), new Vector2(0.86f, 0.60f));
            _fragmentsText.alignment = TextAnchor.UpperLeft;
            _fragmentsText.color = Color.white;

            MakeButton("PlayAgainButton", "Jogar novamente",
                new Vector2(0.20f, 0.07f), new Vector2(0.48f, 0.19f), PlayAgain);
            MakeButton("QuitButton", "Sair",
                new Vector2(0.52f, 0.07f), new Vector2(0.80f, 0.19f), Quit);
        }

        private void FillContent()
        {
            int total = Mathf.Max(_fragments.Count, 0);
            _summaryText.text = $"Sinal com a Terra: 100%   |   Fragmentos de conhecimento: {total}/5";

            var sb = new System.Text.StringBuilder();
            foreach (string planet in _fragments)
            {
                string fact;
                if (!Facts.TryGetValue(Normalize(planet), out fact)) fact = "Fragmento de conhecimento coletado.";
                sb.AppendLine($"•  {planet}: {fact}");
                sb.AppendLine();
            }
            _fragmentsText.text = sb.ToString();
        }

        private static string Normalize(string name)
        {
            string lower = name.ToLowerInvariant();
            return lower.Replace("ú", "u").Replace("é", "e").Replace("í", "i").Replace("ã", "a").Replace("ç", "c");
        }

        private Text MakeLabel(string name, string content, int size, FontStyle style, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panelRoot.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = style;
            text.raycastTarget = false;
            UiTextTuning.Apply(text, size);
            return text;
        }

        private void MakeButton(string name, string label, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_panelRoot.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = new Color(0.2f, 0.4f, 0.6f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = MakeLabelOn(go.transform, label);
            text.color = Color.white;
        }

        private Text MakeLabelOn(Transform parent, string content)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.text = content;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.raycastTarget = false;
            UiTextTuning.Apply(text, 38);
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // --------------------------------------------------------------- Acoes

        private void PlayAgain()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void Quit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
