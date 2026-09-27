namespace Lumen.Core
{
    using UnityEngine;

    /// <summary>
    /// Numeros de direcao, camera e consumo do LUMEN em UM lugar so.
    ///
    /// Eles vivem aqui, e nao apenas como default de um [SerializeField], porque
    /// valor serializado fica gravado dentro do componente: mudar o default do
    /// script nao altera uma cena que ja foi montada. Foi exatamente esse o
    /// motivo de o ajuste de combustivel "nao funcionar" - a cena continuava
    /// com 2.2/s enquanto o script ja dizia 0.2/s.
    ///
    /// Os componentes reaplicam estes valores no Awake/Start, entao o jogo sempre
    /// roda com o tuning oficial. Para mudar a sensabilidade, mexe AQUI (e no
    /// construtor de cena, que reaplica os mesmos numeros no Inspector).
    /// </summary>
    public static class GameTuning
    {
        // --- Combustivel ---
        // Calibrado pela rota: o trecho mais longo (Jupiter -> Marte) tem ~2.725
        // unidades = ~303 s a 9 u/s => ~61 de gasto num tanque de 100, ou seja
        // ~39% de folga. Parado nao gasta nada.
        public const float MaxEnergy = 100f;
        public const float ThrustDrainPerSecond = 0.2f;
        public const float IdleDrainPerSecond = 0f;

        // --- Direcao ---
        // Aceleracao alta (a nave responde na hora), freio forte ao soltar o
        // acelerador e freio de manobra. A nave aponta para onde esta indo, mas
        // isso nunca vira a tela: a camera tem orientacao fixa (ver CameraOffset).
        public const float Acceleration = 26f;
        public const float MaxSpeed = 9f;
        public const float BoostSpeed = 15f;
        public const float CoastDamping = 7f;
        public const float BrakeDamping = 16f;

        /// <summary>Quao rapido a nave gira para apontar o rumo do movimento (suavizacao exponencial).</summary>
        public const float RotationSmooth = 8f;

        // --- Camera ---
        // Enquadramento FIXO no mundo: a camera nao gira com a nave. Esse e o
        // ponto que matou o sintoma antigo - a camera usava
        // target.TransformDirection(offset), entao qualquer giro da nave girava
        // a tela junto e o mundo parecia "voltar para o centro" sozinho.
        // Com o offset fixo, W sobe na tela, D vai para a direita, e nada gira.
        public static readonly Vector3 CameraOffset = new Vector3(0f, 32f, -28f);
        public const float CameraPositionSmooth = 0.16f;

        // --- Mundo ---
        public const float OpenWorldRadius = 4500f;
    }
}
