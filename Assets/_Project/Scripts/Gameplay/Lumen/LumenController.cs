using UnityEngine;
using Lumen.Core;
using Lumen.Cameras;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Movimento do LUMEN em terceira pessoa: A/D GIRAM a nave (o rumo fica
    /// guardado), W/S avancam/recuam para onde ela aponta, Space/Ctrl sobem/descem,
    /// X freia, Shift e boost. Sem fisica de Rigidbody.
    ///
    /// Quando o combustivel acaba a nave PARA no lugar e a tecla RescueKey devolve
    /// o LUMEN ao ultimo ponto seguro com o tanque cheio, para a partida nunca
    /// travar no meio do nada.
    /// </summary>
    [RequireComponent(typeof(LumenEnergySystem))]
    public class LumenController : MonoBehaviour
    {
        /// <summary>Tecla de resgate. O HUD cita essa tecla na mensagem de alerta.</summary>
        public const KeyCode RescueKey = KeyCode.R;

        /// <summary>Freio de manobra. Derruba a velocidade na hora, sem gastar combustivel.</summary>
        public const KeyCode BrakeKey = KeyCode.X;

        [Header("Movimento")]
        [SerializeField] private float acceleration = GameTuning.Acceleration;
        [SerializeField] private float maxSpeed = GameTuning.MaxSpeed;
        [SerializeField] private float boostSpeed = GameTuning.BoostSpeed;
        [Tooltip("Freio ao soltar o acelerador. Alto = a nave para na hora em vez de patinar.")]
        [SerializeField] private float coastDamping = GameTuning.CoastDamping;
        [Tooltip("Freio da tecla X: derruba a velocidade na hora, para manobrar em espaço apertado.")]
        [SerializeField] private float brakeDamping = GameTuning.BrakeDamping;
        [Tooltip("Suavização da curva (A/D). Maior = responde mais rápido.")]
        [SerializeField] private float rotationSmooth = GameTuning.RotationSmooth;
        [Tooltip("Graus por segundo ao segurar A/D.")]
        [SerializeField] private float turnSpeed = GameTuning.TurnSpeed;

        [Header("Limites de voo")]
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private float boundsRadius = 45f;
        [SerializeField] private float openWorldRadius = 4500f;

        // Velocidade guardada em dois eixos: "para frente" (relativo ao rumo) e
        // vertical. Assim, ao girar, a nave segue o novo rumo sem derrapar.
        private float _forwardSpeed;
        private float _verticalSpeed;
        private float _yaw;
        private float _turnInput;

        private LumenEnergySystem _energy;
        private bool _boundsOpened;
        private bool _boostEnabled;

#if !ENABLE_LEGACY_INPUT_MANAGER
        private bool _loggedInputWarning;

        private void Update()
        {
            if (_loggedInputWarning) return;
            _loggedInputWarning = true;
            Debug.LogError("[LUMEN] O 'Input Manager (Old)' está desabilitado, por isso o LUMEN não responde. " +
                           "Vá em Edit > Project Settings > Player > Active Input Handling e escolha " +
                           "'Input Manager (Old)' ou 'Both', depois reinicie o Editor.");
        }
#else
        private void Awake()
        {
            _energy = GetComponent<LumenEnergySystem>();

            // O tuning oficial sempre vence o valor salvo no Inspector.
            ApplyMovementTuning(GameTuning.Acceleration, GameTuning.MaxSpeed, GameTuning.BoostSpeed,
                GameTuning.CoastDamping, GameTuning.BrakeDamping, GameTuning.RotationSmooth);
            turnSpeed = GameTuning.TurnSpeed;
            openWorldRadius = GameTuning.OpenWorldRadius;
        }

        private void Start()
        {
            _yaw = transform.eulerAngles.y;

            // Enquanto nenhum planeta for concluido, o resgate devolve a nave
            // para onde ela comecou - assim nunca existe um estado sem saida.
            if (GameManager.Instance != null && !GameManager.Instance.HasRescuePoint)
                GameManager.Instance.SetRescuePoint(transform.position, "o início da rota");
        }

        private void OnEnable()
        {
            EventBus.OnTutorialCompleted += HandleTutorialCompleted;
        }

        private void OnDisable()
        {
            EventBus.OnTutorialCompleted -= HandleTutorialCompleted;
        }

        // Boost (Shift) so libera depois do tutorial terminar.
        private void HandleTutorialCompleted()
        {
            _boostEnabled = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return; // controle travado no menu, intro, desafio ou pausa

            // Assim que a intro termina, o mapa inteiro fica aberto.
            if (!_boundsOpened)
            {
                _boundsOpened = true;
                boundsRadius = Mathf.Max(boundsRadius, openWorldRadius);
            }

            // Sem combustivel a nave nao se locomove: ela para e o resgate assume.
            if (_energy.IsEmpty)
            {
                HandleOutOfFuel();
                return;
            }

            float dt = Time.deltaTime;

            // --- Giro (A/D): o rumo escolhido fica guardado ---
            float turnRaw = Input.GetAxisRaw("Horizontal");
            _turnInput = Mathf.Lerp(_turnInput, turnRaw, 1f - Mathf.Exp(-rotationSmooth * dt));
            _yaw += _turnInput * turnSpeed * dt;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // --- Propulsao (W/S = frente/tras, Space/Ctrl = sobe/desce) ---
            float thrustZ = Input.GetAxisRaw("Vertical");
            float thrustY = 0f;
            if (Input.GetKey(KeyCode.Space)) thrustY += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) thrustY -= 1f;

            bool isBraking = Input.GetKey(BrakeKey);
            bool isThrusting = Mathf.Abs(thrustZ) > 0.01f || Mathf.Abs(thrustY) > 0.01f;

            if (!isBraking)
            {
                _forwardSpeed += thrustZ * acceleration * dt;
                _verticalSpeed += thrustY * acceleration * dt;
            }

            // Freio por eixo, independente de FPS: soltar a tecla PARA a nave.
            float dampZ = isBraking ? brakeDamping : (Mathf.Abs(thrustZ) > 0.01f ? 0f : coastDamping);
            float dampY = isBraking ? brakeDamping : (Mathf.Abs(thrustY) > 0.01f ? 0f : coastDamping);
            _forwardSpeed *= Mathf.Exp(-dampZ * dt);
            _verticalSpeed *= Mathf.Exp(-dampY * dt);

            float cap = (_boostEnabled && Input.GetKey(KeyCode.LeftShift)) ? boostSpeed : maxSpeed;
            float mag = new Vector2(_forwardSpeed, _verticalSpeed).magnitude;
            if (mag > cap)
            {
                float k = cap / mag;
                _forwardSpeed *= k;
                _verticalSpeed *= k;
            }

            Vector3 velocity = transform.forward * _forwardSpeed + Vector3.up * _verticalSpeed;
            transform.position = ClampToBounds(transform.position + velocity * dt);

            // Frear e girar nao queimam combustivel.
            _energy.Tick(dt, isThrusting && !isBraking);
        }

        /// <summary>
        /// Combustivel zerado: a nave fica parada esperando o jogador apertar
        /// RescueKey. Nao existe temporizador.
        /// </summary>
        private void HandleOutOfFuel()
        {
            _forwardSpeed = 0f;
            _verticalSpeed = 0f;
            if (Input.GetKeyDown(RescueKey)) Rescue();
        }

        /// <summary>
        /// Devolve o LUMEN ao ultimo ponto seguro com o tanque cheio.
        /// Nao perde fragmentos nem progresso.
        /// </summary>
        private void Rescue()
        {
            _forwardSpeed = 0f;
            _verticalSpeed = 0f;

            string place = "a base";
            if (GameManager.Instance != null && GameManager.Instance.HasRescuePoint)
            {
                place = GameManager.Instance.RescueLabel;
                transform.position = GameManager.Instance.RescuePosition;
            }

            _energy.RefillToFull();

            // A camera estava la longe: sem isso o SmoothDamp atravessa o mapa.
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.SnapToTarget();

            EventBus.RaiseRescued(place);
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            Vector3 offset = position - boundsCenter;
            float mag = offset.magnitude;
            if (mag <= boundsRadius) return position;

            // Borda do mapa: segura a nave ali (o jogador pode girar e voltar).
            _forwardSpeed = 0f;
            _verticalSpeed = 0f;
            return boundsCenter + offset.normalized * boundsRadius;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            Gizmos.DrawWireSphere(boundsCenter, boundsRadius);
        }

        /// <summary>Aumenta o raio de voo permitido - usado ao sair do tutorial.</summary>
        public void ExpandBounds(float newRadius)
        {
            boundsRadius = Mathf.Max(boundsRadius, newRadius);
        }

        /// <summary>Garante que o raio aberto (pos-intro) acompanhe o tamanho do mundo construido.</summary>
        public void SetOpenWorldRadius(float radius)
        {
            openWorldRadius = radius;
        }

        /// <summary>Aplica os numeros de movimento (chamado tambem pelo construtor de cena).</summary>
        public void ApplyMovementTuning(float newAcceleration, float newMaxSpeed, float newBoostSpeed,
            float newCoastDamping, float newBrakeDamping, float newRotationSmooth)
        {
            acceleration = newAcceleration;
            maxSpeed = newMaxSpeed;
            boostSpeed = newBoostSpeed;
            coastDamping = newCoastDamping;
            brakeDamping = newBrakeDamping;
            rotationSmooth = newRotationSmooth;
        }
    }
}
