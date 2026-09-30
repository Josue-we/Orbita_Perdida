namespace Lumen.Core
{
    using UnityEngine;

    /// <summary>
    /// Numeros de direcao, camera e consumo do LUMEN em UM lugar so.
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
        // (Saturno -> Jupiter, ~1600 unidades) demora ~178s na velocidade maxima
        // SEM boost. A 0.45/s isso gasta ~80 do tanque de 100, ou seja, o jogador
        // SEMPRE chega no proximo planeta, mas com so ~20% de combustivel.
        // Boostar o trecho inteiro gasta MENOS combustivel total (o dreno e por
        // segundo, nao por distancia - andar mais rapido leva menos tempo
        // acelerando), entao usar o boost o tempo todo deixa mais folga que isso.
        public const float MaxEnergy = 100f;
        public const float ThrustDrainPerSecond = 0.45f;
        public const float IdleDrainPerSecond = 0f;

        // --- Direcao ---
        public const float Acceleration = 26f;
        public const float MaxSpeed = 9f;
        // Boost bem mais rapido que o normal (quase 2.7x) - liberado ao concluir o tutorial.
        public const float BoostSpeed = 24f;
        public const float CoastDamping = 7f;
        public const float BrakeDamping = 16f;

        /// <summary>Suavizacao da curva (A/D): maior = responde mais rapido.</summary>
        public const float RotationSmooth = 8f;

        /// <summary>Graus por segundo ao segurar A/D.</summary>
        public const float TurnSpeed = 100f;

        // --- Camera (terceira pessoa, atras da nave) ---
        public static readonly Vector3 CameraOffset = new Vector3(0f, 4f, -12f);
        public static readonly Vector3 CameraLookAhead = new Vector3(0f, 1f, 12f);
        public const float CameraPositionSmooth = 0.16f;
        public const float CameraRotationSmooth = 6f;

        // --- Mundo ---
        public const float OpenWorldRadius = 4500f;
    }
}
