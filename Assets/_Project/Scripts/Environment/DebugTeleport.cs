#if UNITY_EDITOR
using UnityEngine;

namespace Lumen.Environment
{
    /// <summary>Atalho SO DO EDITOR para testar: F1 Saturno, F2 Jupiter, F3 Marte, F4 Terra, F5 olhando para o Sol, F6 perto do proximo planeta da rota. Nao entra no build.</summary>
    public static class DebugTeleport
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("DebugTeleport");
            go.AddComponent<Runner>();
        }

        private class Runner : MonoBehaviour
        {
            private void Update()
            {
                Vector3? p = null;
                if (Input.GetKeyDown(KeyCode.F1)) p = new Vector3(-900f, 180f, 6850f);
                if (Input.GetKeyDown(KeyCode.F2)) p = new Vector3(2500f, 80f, 12150f);
                if (Input.GetKeyDown(KeyCode.F3)) p = new Vector3(100f, 40f, 15600f);
                if (Input.GetKeyDown(KeyCode.F4)) p = new Vector3(1000f, 20f, 16600f);
                if (Input.GetKeyDown(KeyCode.F6))
                {
                    foreach (var t in FindObjectsByType<Lumen.Gameplay.PlanetApproachTrigger>(FindObjectsSortMode.None))
                    {
                        if (t.RouteIndex != Lumen.Core.EventBus.RouteProgress) continue;
                        var col = t.GetComponent<SphereCollider>();
                        float r = col != null ? col.radius * t.transform.lossyScale.x : 200f;
                        p = t.transform.position + Vector3.back * (r + 150f);
                    }
                }
                bool sun = Input.GetKeyDown(KeyCode.F5);
                if (sun) p = new Vector3(8500f, 400f, 8000f);
                if (!p.HasValue) return;
                var ship = FindFirstObjectByType<Lumen.Gameplay.LumenController>();
                if (ship != null) {
                    ship.transform.position = p.Value;
                    if (sun) ship.transform.rotation = Quaternion.LookRotation(Vector3.right);
                }
            }
        }
    }
}
#endif
