using UnityEngine;

namespace Lumen.Environment
{
    /// <summary>
    /// Rotacao lenta e continua, usada nos planetas decorativos de fundo.
    /// Custo praticamente zero: so gira o transform, sem fisica envolvida.
    /// </summary>
    public class SlowRotator : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 2f;

        private void Update()
        {
            transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
