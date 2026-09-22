using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Movimento do LUMEN: aceleracao, desaceleracao e rotacao suave em direcao
    /// ao movimento. Controle por teclado (WASD + Space/Ctrl + Shift p/ boost).
    /// O limite de voo e suave (empurra de volta em vez de travar de vez) e e
    /// expandido ao concluir o tutorial. Sem fisica de Rigidbody: movimento
    /// cinematico leve, previsivel de depurar.
    /// </summary>
    [RequireComponent(typeof(LumenEnergySystem))]
    public class LumenController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private float boostSpeed = 14f;
        [SerializeField] private float damping = 4f;
        [SerializeField] private float rotationSpeed = 6f;

        [Header("Limites de voo")]
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private float boundsRadius = 45f;
        [SerializeField] private float openWorldRadius = 900f;

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
            Debug.LogError("[LUMEN] O 'Input Manager (Old)' esta desabilitado, por isso o LUMEN nao responde. " +
                           "Va em Edit > Project Settings > Player > Active Input Handling e escolha " +
                           "'Input Manager (Old)' ou 'Both', depois reinicie o Editor.");
        }
#else
        private void Awake()
        {
            _energy = GetComponent<LumenEnergySystem>();
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

            Vector3 input = ReadInput();
            bool isThrusting = input.sqrMagnitude > 0.0001f;

            if (isThrusting)
            {
                _velocity += input.normalized * acceleration * Time.deltaTime;
                float cap = (_boostEnabled && Input.GetKey(KeyCode.LeftShift)) ? boostSpeed : maxSpeed;
                _velocity = Vector3.ClampMagnitude(_velocity, cap);
            }
            else
            {
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, damping * Time.deltaTime);
            }

            transform.position = ClampToBounds(transform.position + _velocity * Time.deltaTime);

            if (_velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            _energy.Tick(Time.deltaTime, isThrusting);
        }

        private Vector3 ReadInput()
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
    }
}