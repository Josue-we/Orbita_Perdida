#if UNITY_EDITOR
using UnityEngine;

namespace Lumen.Environment
{
    /// <summary>Atalho SO DO EDITOR para testar: F1 Saturno, F2 Jupiter, F3 Marte, F4 Terra. Nao entra no build.</summary>
    public static class DebugTeleport
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("DebugTeleport");
            go.hideFlags = HideFlags.DontSave;
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
                if (!p.HasValue) return;
                var ship = FindFirstObjectByType<Lumen.Gameplay.LumenController>();
                if (ship != null) ship.transform.position = p.Value;
            }
        }
    }
}
#endif
