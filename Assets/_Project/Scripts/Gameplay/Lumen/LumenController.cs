using UnityEngine;
using Lumen.Core;

namespace Lumen.Gameplay
{
    /// <summary>
    /// Movimento do LUMEN: aceleracao, desaceleracao e rotacao suave em direcao
    /// ao movimento. Controle apenas por teclado (WASD + Space/Ctrl), conforme
    /// decidido para o projeto. Sem fisica de Rigidbody: e um movimento cinematico
    /// simples, leve para a Iris Xe e mais previsivel para depurar.
    /// </summary>
    [RequireComponent(typeof(LumenEnergySystem))]
    public class LumenController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] private float acceleration = 12f;
        [SerializeField] private float maxSpeed = 8f;
        [SerializeField] private float damping = 4f;
        [SerializeField] private float rotationSpeed = 6f;

        [Header("Limites da zona de tutorial")]
        [SerializeField] private Vector3 boundsCenter = Vector3.zero;
        [SerializeField] private float boundsRadius = 40f;

        private Vector3 _velocity;
        private LumenEnergySystem _energy;

        private void Awake()
        {
            _energy = GetComponent<LumenEnergySystem>();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return; // controle travado durante a intro (ou uma futura pausa)

            Vector3 input = ReadInput();
            bool isThrusting = input.sqrMagnitude > 0.0001f;

            if (isThrusting)
            {
                _velocity += input.normalized * acceleration * Time.deltaTime;
                _velocity = Vector3.ClampMagnitude(_velocity, maxSpeed);
            }
            else
            {
                _velocity = Vector3.Lerp(_velocity, Vector3.zero, damping * Time.deltaTime);
            }

            Vector3 nextPosition = ClampToBounds(transform.position + _velocity * Time.deltaTime);
            transform.position = nextPosition;

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
            if (offset.magnitude > boundsRadius)
            {
                offset = offset.normalized * boundsRadius;
                _velocity = Vector3.zero;
            }
            return boundsCenter + offset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.3f);
            Gizmos.DrawWireSphere(boundsCenter, boundsRadius);
        }
    }
}
