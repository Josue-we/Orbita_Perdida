using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;

namespace Lumen.UI
{
    /// <summary>
    /// ESC durante o jogo abre "Deseja realmente sair do jogo?". Sim fecha o
    /// jogo; Nao (ou ESC de novo) volta de onde parou.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private bool _open;
        private bool _wasPlayable;
        private GameState _previousState = GameState.Playing;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (yesButton != null) yesButton.onClick.AddListener(QuitGame);
            if (noButton != null) noButton.onClick.AddListener(Close);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            GameState state = gm.CurrentState;
            bool playable = state == GameState.Playing || state == GameState.Challenge;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_open) Close();
                // _wasPlayable evita abrir no mesmo frame em que o ESC pulou a intro.
                else if (playable && _wasPlayable) Open(state);
            }

            _wasPlayable = playable;
        }

        private void Open(GameState current)
        {
            _open = true;
            _previousState = current;
            GameManager.Instance.SetState(GameState.Paused);
            Time.timeScale = 0f;
            if (panelRoot != null) panelRoot.SetActive(true);
        }

        private void Close()
        {
            _open = false;
            Time.timeScale = 1f;
            if (panelRoot != null) panelRoot.SetActive(false);
            GameManager.Instance.SetState(_previousState);
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // no Editor, sai do Play Mode
#else
            Application.Quit();
#endif
        }

        public void Configure(GameObject panel, Button yes, Button no)
        {
            panelRoot = panel;
            yesButton = yes;
            noButton = no;
        }
    }
}
