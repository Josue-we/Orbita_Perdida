using UnityEngine;

namespace Lumen.Cameras
{
    /// <summary>
    /// Camera de perseguicao leve (sem Cinemachine): segue o LUMEN com suavizacao
    /// via SmoothDamp. Simples de depurar para quem esta comecando na Unity;
    /// pode ser trocada por Cinemachine depois, se quisermos transicoes
    /// cinematograficas mais ricas nas aproximacoes de planeta.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -8f);
        [SerializeField] private float positionSmoothTime = 0.25f;
        [SerializeField] private float rotationSpeed = 3f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + target.TransformDirection(offset);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _velocity, positionSmoothTime);

            Quaternion desiredRotation = Quaternion.LookRotation(target.position - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
