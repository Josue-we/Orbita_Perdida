#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Ao entrar em Play Mode a Game View normalmente NAO tem o foco de teclado/mouse
    /// (o foco fica na barra de ferramentas, de onde voce clicou em Play). Isso faz o
    /// PRIMEIRO clique dentro do jogo servir so para "focar" a janela, sem chegar no
    /// EventSystem - por isso era preciso clicar duas vezes no botao "Jogar" (a musica
    /// ja estava tocando desde o carregamento da cena, entao parecia que "algo"
    /// acontecia no primeiro clique, mas nao era o botao).
    ///
    /// Aqui a Game View e focada automaticamente assim que o Play Mode comeca, entao
    /// o primeiro clique real do jogador ja registra. So roda no Editor (nunca cria
    /// uma janela nova, so foca uma ja existente) e nao afeta o build.
    /// </summary>
    [InitializeOnLoad]
    public static class AutoFocusGameView
    {
        static AutoFocusGameView()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;

            var gameViewType = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;

            var windows = Resources.FindObjectsOfTypeAll(gameViewType);
            if (windows.Length == 0) return;

            (windows[0] as EditorWindow)?.Focus();
        }
    }
}
#endif
