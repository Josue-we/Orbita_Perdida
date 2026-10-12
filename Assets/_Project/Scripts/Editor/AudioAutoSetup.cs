#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Lumen.Audio;

namespace Lumen.EditorTools
{
    /// <summary>
    /// Ao apertar Play, confere o AudioManager da cena e CORRIGE o que estiver errado
    /// (musica apontando para efeito sonoro, loops da nave sem clipe...). Assim o audio
    /// nao depende de lembrar de rodar o menu de montagem depois de atualizar os arquivos.
    /// Roda so no Editor; nao afeta o build.
    /// </summary>
    [InitializeOnLoad]
    public static class AudioAutoSetup
    {
        static AudioAutoSetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            var manager = Object.FindFirstObjectByType<AudioManager>();
            if (manager == null) return;

            AudioClip menu = manager.MenuMusicClip, game = manager.GameplayMusicClip;
            AudioClip thrust = manager.ThrustLoopClip, boost = manager.BoostLoopClip;

            TutorialSceneBuilder.ApplyAudioAssignments(manager);

            bool changed = menu != manager.MenuMusicClip || game != manager.GameplayMusicClip ||
                           thrust != manager.ThrustLoopClip || boost != manager.BoostLoopClip;
            if (!changed) return;

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}
#endif
