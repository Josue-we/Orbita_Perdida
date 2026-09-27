using UnityEngine;
using Lumen.Core;
using Lumen.Cameras;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Movimento do LUMEN em estilo arcade com intencao: aceleracao forte,
    /// freio ao soltar o acelerador e freio de manobra. A nave aponta para onde
    /// esta indo, como um aviao - e isso nao vira a tela, porque a camera tem
    /// orientacao fixa no mundo (ver CameraFollow). Sem fisica de Rigidbody.
    /// Teclado: WASD move, Space/Ctrl sobe/desce, X freia, Shift e boost.
    /// O limite de voo e suave (empurra de volta em vez de travar de vez) e
    /// expandido ao concluir o tutorial.
    ///
    /// Quando o combustivel acaba a nave PARA no lugar e o resgate assume:
    /// a tecla RescueKey devolve o LUMEN ao ultimo ponto seguro com o tanque
    /// cheio, para a partida nunca travar no meio do nada.
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
        [Tooltip("Quão rápido a nave gira para apontar o rumo. Não afeta a câmera.")]
        [SerializeField] private float rotationSmooth = GameTuning.RotationSmooth;

        [Header("Limites de voo")]
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private float boundsRadius = 45f;
        [SerializeField] private float openWorldRadius = 4500f;

        private Vector3 _velocity;
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

            // O tuning oficial sempre vence o valor salvo no Inspector - e o que
            // faz o ajuste valer em qualquer cena, montada antes ou depois dele.
            ApplyMovementTuning(GameTuning.Acceleration, GameTuning.MaxSpeed, GameTuning.BoostSpeed,
                GameTuning.CoastDamping, GameTuning.BrakeDamping, GameTuning.RotationSmooth);
            openWorldRadius = GameTuning.OpenWorldRadius;
        }

        private void Start()
        {
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

        // Boost (Shift) so libera depois do tutorial terminar - mais uma marca
        // clara de que o jogo "continuou" de fato.
        private void HandleTutorialCompleted()
        {
            _boostEnabled = true;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return; // controle travado durante a intro, um desafio ou uma futura pausa

            // Assim que a intro termina, o mapa inteiro fica aberto. O gatilho de
            // saida do tutorial so marca o "tutorial concluido" (seta de navegacao) -
            // ele NAO e mais necessario para liberar o caminho ate o planeta.
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

            Vector3 input = ReadMoveInput();
            bool isThrusting = input.sqrMagnitude > 0.0001f;
            bool isBraking = Input.GetKey(BrakeKey);

            if (isThrusting && !isBraking)
                _velocity += input.normalized * acceleration * Time.deltaTime;

            // Freio independente de FPS: soltar o acelerador agora PARA a nave
            // (antes ela deslizava e a manobra ficava "engrudada").
            float damping = isBraking ? brakeDamping : (isThrusting ? 0f : coastDamping);
            if (damping > 0f)
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, 1f - Mathf.Exp(-damping * Time.deltaTime));

            float cap = (_boostEnabled && Input.GetKey(KeyCode.LeftShift)) ? boostSpeed : maxSpeed;
            _velocity = Vector3.ClampMagnitude(_velocity, cap);

            transform.position = ClampToBounds(transform.position + _velocity * Time.deltaTime);
            AlignToTravel();

            // Frear nao queima combustivel: e o contrario de acelerar.
            _energy.Tick(Time.deltaTime, isThrusting && !isBraking);
        }

        /// <summary>
        /// Combustivel zerado: zera a velocidade (a nave fica parada) e espera o
        /// jogador apertar RescueKey. Nao existe temporizador - voltar ou nao e
        /// decisao dele, e o unico jeito de destravar a nave.
        /// </summary>
        private void HandleOutOfFuel()
        {
            _velocity = Vector3.zero;
            if (Input.GetKeyDown(RescueKey)) Rescue();
        }

        /// <summary>
        /// Devolve o LUMEN ao ultimo ponto seguro (planeta ja resolvido ou o
        /// inicio da rota) com o tanque cheio. Nao perde fragmentos nem progresso.
        /// </summary>
        private void Rescue()
        {
            _velocity = Vector3.zero;

            string place = "a base";
            if (GameManager.Instance != null && GameManager.Instance.HasRescuePoint)
            {
                place = GameManager.Instance.RescueLabel;
                transform.position = GameManager.Instance.RescuePosition;
            }

            _energy.RefillToFull();

            // A camera estava la longe: sem isso o SmoothDamp atravessa o mapa
            // inteiro em camera lenta.
            var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.SnapToTarget();

            EventBus.RaiseRescued(place);
        }

        /// <summary>
        /// A nave aponta para onde esta indo - como um aviao. Isso ja existiu e
        /// o jogador achava estranho, mas a causa nao era a nave: era a camera,
        /// que girava junto com ela. Agora a camera tem orientacao fixa, e como
        /// o freio zera a velocidade ao soltar a tecla, a nave ja parou antes de
        /// alinhar - nao existe mais o "vira sozinho" no meio do caminho.
        /// </summary>
        private void AlignToTravel()
        {
            if (_velocity.sqrMagnitude < 0.04f) return;

            Quaternion target = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
            float t = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
        }

        private Vector3 ReadMoveInput()
        {
            float x = Input.GetAxisRaw("Horizontal"); // A / D
            float z = Input.GetAxisRaw("Vertical");    // W / S
            float y = 0f;

            if (Input.GetKey(KeyCode.Space)) y += 1f;
            if (Input.GetKey(KeyCode.LeftControl)) y -= 1f;

            return new Vector3(x, y, z);
        }

        private Vector3 ClampToBounds(Vector3 position)
        {
            Vector3 offset = position - boundsCenter;
            float mag = offset.magnitude;
            if (mag <= boundsRadius) return position;

            // Limite suave: segura na borda mantendo o componente de velocidade
            // que aponta de volta para dentro, para nunca travar o jogador.
            Vector3 inward = -offset.normalized;
            position = boundsCenter + offset.normalized * boundsRadius;
            _velocity = Vector3.Project(_velocity, inward);
            return position;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            Gizmos.DrawWireSphere(boundsCenter, boundsRadius);
        }

        /// <summary>Aumenta o raio de voo permitido - usado ao sair do tutorial e liberar o resto do mapa.</summary>
        public void ExpandBounds(float newRadius)
        {
            boundsRadius = Mathf.Max(boundsRadius, newRadius);
        }

        /// <summary>Garante que o raio aberto (pos-intro) acompanhe o tamanho do mundo construido.</summary>
        public void SetOpenWorldRadius(float radius)
        {
            openWorldRadius = radius;
        }

        /// <summary>
        /// Aplica os numeros de movimento. O construtor de cena chama isso TODA
        /// vez que roda, inclusive em cena ja montada: como os campos sao
        /// [SerializeField], o valor fica gravado no componente e mudar apenas
        /// o default do script nao atualiza uma cena existente - foi assim que
        /// o ajuste de combustivel "nao funcionou" antes.
        /// </summary>
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