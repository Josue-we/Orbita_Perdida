using UnityEngine;
using UnityEngine.UI;

namespace Lumen.Core
{
    /// <summary>
    /// Aplica tamanho de fonte (e layout) em tempo de execucao a partir do
    /// GameTuning. Assim o ajuste vale em QUALQUER cena, inclusive uma que ja
    /// foi montada antes pelo TutorialSceneBuilder.
    ///
    /// Por que a fonte "nao crescia": os Canvas estavam em "Constant Pixel Size", entao
    /// o texto era do tamanho de pixels da janela da Game View (pequena no Editor).
    /// Agora TODO Canvas da cena vira "Scale With Screen Size" (1920x1080): o texto
    /// acompanha o tamanho da janela e fica grande em qualquer resolucao.
    /// </summary>
    public static class UiTextTuning
    {
        /// <summary>Configura todos os CanvasScaler da cena assim que ela carrega.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ConfigureAllCanvasesOnLoad()
        {
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                ConfigureScaler(scaler);
        }

        public static void ConfigureScaler(CanvasScaler scaler)
        {
            if (scaler == null) return;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = GameTuning.UiReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        /// <summary>Configura o CanvasScaler do Canvas que contem este objeto.</summary>
        public static void ConfigureCanvasOf(Component c)
        {
            if (c == null) return;
            var canvas = c.GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var scaler = canvas.rootCanvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.rootCanvas.gameObject.AddComponent<CanvasScaler>();
            ConfigureScaler(scaler);
        }

        /// <summary>
        /// Define o tamanho da fonte. Best fit fica ligado so como rede de seguranca para
        /// textos muito longos, com piso alto (MinFitFontSize): o texto normal fica no
        /// tamanho pedido.
        /// </summary>
        public static void Apply(Text text, int size, int minSize = -1)
        {
            if (text == null) return;
            if (minSize < 0) minSize = GameTuning.MinFitFontSize;

            ConfigureCanvasOf(text);

            text.fontSize = size;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(minSize, size);
            text.resizeTextMaxSize = size;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public static void SetAnchors(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rt == null) return;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
