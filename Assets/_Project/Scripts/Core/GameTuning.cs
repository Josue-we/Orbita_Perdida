namespace Lumen.Core
{
    using UnityEngine;

    /// <summary>
    /// Numeros de direcao, camera, consumo e texto do LUMEN em UM lugar so.
    ///
    /// Eles vivem aqui, e nao apenas como default de um [SerializeField], porque
    /// valor serializado fica gravado dentro do componente: mudar o default do
    /// script nao altera uma cena que ja foi montada. Os componentes reaplicam
    /// estes valores no Awake/Start, entao o jogo sempre roda com o tuning oficial.
    /// </summary>
    public static class GameTuning
    {
        // --- Combustivel ---
        // Calibrado pela rota (ver TutorialSceneBuilder): o trecho mais longo
        // (Saturno -> Jupiter, ~6300 unidades) demora ~175s na velocidade maxima (36 u/s)
        // SEM boost. A 0.45/s isso gasta ~80 do tanque de 100, ou seja, o jogador
        // SEMPRE chega no proximo planeta, mas com so ~20% de combustivel.
        // Boostar o trecho inteiro gasta MENOS combustivel total (o dreno e por
        // segundo, nao por distancia), entao usar o boost o tempo todo deixa mais folga.
        public const float MaxEnergy = 100f;
        public const float ThrustDrainPerSecond = 0.45f;
        public const float IdleDrainPerSecond = 0f;

        // --- Direcao ---
        public const float Acceleration = 104f;   // 4x
        public const float MaxSpeed = 36f;       // 4x
        public const float BoostSpeed = 96f;     // 4x
        public const float CoastDamping = 7f;
        public const float BrakeDamping = 16f;

        /// <summary>Suavizacao da curva (A/D): maior = responde mais rapido.</summary>
        public const float RotationSmooth = 8f;

        /// <summary>Graus por segundo ao segurar A/D.</summary>
        public const float TurnSpeed = 100f;

        // --- Camera (terceira pessoa, atras da nave) ---
        public static readonly Vector3 CameraOffset = new Vector3(0f, 4f, -12f);
        public static readonly Vector3 CameraLookAhead = new Vector3(0f, 1f, 12f);
        public const float CameraPositionSmooth = 0.06f; // curto: com 4x de velocidade a camera nao pode ficar para tras
        public const float CameraRotationSmooth = 6f;

        // --- Mundo ---
        public const float OpenWorldRadius = 30000f; // rota ~17.000 + Sol e planetas decorativos

        // --- Sinal com a Terra (HUD) ---
        // O sinal NAO depende de distancia: so sobe, a cada fragmento coletado.
        // 5% + 5 fragmentos x 18% = 95%; ao chegar na Terra fecha em 100%.
        public const float SignalBase = 5f;
        public const float SignalPerFragment = 18f;

        // --- Texto ---
        // Os Canvas escalam com a tela (referencia 1920x1080, ver UiTextTuning), entao estes
        // tamanhos valem em qualquer resolucao. Sao os tamanhos REAIS (sem encolher).
        public static readonly Vector2 UiReferenceResolution = new Vector2(1920f, 1080f);

        public const int IntroFontSize = 54;
        public const int HudLabelFontSize = 28;
        public const int HudPercentFontSize = 26;
        public const int HudObjectiveFontSize = 32;
        public const int HudFragmentsFontSize = 36;
        public const int NovaCaptionFontSize = 44;   // curiosidades da NOVA (antes 20)
        public const int QuizQuestionFontSize = 48;  // pergunta (antes 22)
        public const int QuizOptionFontSize = 40;    // alternativas (antes 16)
        public const int QuizFeedbackFontSize = 34;  // acerto/erro (antes 16)

        // Piso do "best fit": so encolhe se um texto MUITO longo nao couber na caixa.
        public const int MinFitFontSize = 28;

        // Caixa da NOVA: fica colada acima do objetivo do HUD, centralizada, e a ALTURA se ajusta
        // ao texto (NovaNarrationController). Estes numeros sao so o limite.
        public const float NovaPanelCenterX = 0.5f;
        public const float NovaPanelWidth = 0.72f;      // fracao da largura da tela
        public const float NovaPanelBottom = 0.19f;     // fracao da altura (acima do objetivo do HUD)
        public const float NovaPanelMaxHeight = 0.30f;  // fracao da altura da tela
        public const float NovaPanelPadding = 20f;      // unidades de canvas em volta do texto

        // Compat: usados na montagem inicial da cena.
        public static readonly Vector2 NovaPanelAnchorMin = new Vector2(0.14f, 0.19f);
        public static readonly Vector2 NovaPanelAnchorMax = new Vector2(0.86f, 0.30f);

        // Quiz: painel abaixo das barras do HUD e acima do objetivo, sem invadir nenhum dos dois.
        public static readonly Vector2 QuizPanelAnchorMin = new Vector2(0.14f, 0.17f);
        public static readonly Vector2 QuizPanelAnchorMax = new Vector2(0.86f, 0.76f);

        // Layout interno do painel do quiz (fracoes do painel).
        public static readonly Vector2 QuizQuestionMin = new Vector2(0.04f, 0.70f);
        public static readonly Vector2 QuizQuestionMax = new Vector2(0.96f, 0.99f);
        public static readonly float[] QuizOptionTops = { 0.68f, 0.49f, 0.30f };
        public const float QuizOptionHeight = 0.17f;
        public static readonly Vector2 QuizFeedbackMin = new Vector2(0.04f, 0.01f);
        public static readonly Vector2 QuizFeedbackMax = new Vector2(0.96f, 0.12f);

        // --- Som da nave (volumes oficiais; o turbo e mais suave que o ronco normal) ---
        public const float ThrustVolume = 0.30f;
        public const float BoostVolume = 0.30f;
    }
}
