using UnityEngine;
using Lumen.Core;

namespace Lumen.Cameras
{
    /// <summary>
    /// Camera de perseguicao ATRAS da nave. Segue apenas o giro horizontal (yaw)
    /// da nave, entao o horizonte nunca inclina, e olha um ponto a frente dela.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 4f, -12f);
        [SerializeField] private Vector3 lookAhead = new Vector3(0f, 1f, 12f);
        [SerializeField] private float positionSmoothTime = 0.16f;
        [SerializeField] private float rotationSmooth = 6f;

        private Vector3 _velocity;

        private void Awake()
        {
            // Tuning oficial sempre vence o valor salvo no Inspector (ver GameTuning).
            offset = GameTuning.CameraOffset;
            lookAhead = GameTuning.CameraLookAhead;
            positionSmoothTime = GameTuning.CameraPositionSmooth;
            rotationSmooth = GameTuning.CameraRotationSmooth;
        }

        private void Start()
        {
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            Vector3 desiredPosition = target.position + yaw * offset;

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition,
                ref _velocity, positionSmoothTime);

            float t = 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                DesiredRotation(yaw, desiredPosition), t);
        }

        private Quaternion DesiredRotation(Quaternion yaw, Vector3 fromPosition)
        {
            Vector3 lookPoint = target.position + yaw * lookAhead;
            Vector3 dir = lookPoint - fromPosition;
            return dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir, Vector3.up) : transform.rotation;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        /// <summary>Cola a camera atras da nave na hora (inicio e resgate).</summary>
        public void SnapToTarget()
        {
            if (target == null) return;

            Quaternion yaw = Quaternion.Euler(0f, target.eulerAngles.y, 0f);
            Vector3 pos = target.position + yaw * offset;
            transform.position = pos;
            transform.rotation = DesiredRotation(yaw, pos);
            _velocity = Vector3.zero;
        }
    }
}
