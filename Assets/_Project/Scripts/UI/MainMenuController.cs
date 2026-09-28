using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;

namespace Lumen.UI
{
    /// <summary>Tela inicial: titulo + botao "Jogar". Ao clicar, libera a intro.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button playButton;

        private void Start()
        {
            if (panelRoot != null) panelRoot.SetActive(true);
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
        }

        private void Update()
        {
            // Atalho: Enter tambem inicia.
            if (panelRoot != null && panelRoot.activeSelf && Input.GetKeyDown(KeyCode.Return))
                OnPlay();
        }

        private void OnPlay()
        {
            if (panelRoot != null) panelRoot.SetActive(false);
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Intro);
        }

        public void Configure(GameObject panel, Button play)
        {
            panelRoot = panel;
            playButton = play;
        }
    }
}
