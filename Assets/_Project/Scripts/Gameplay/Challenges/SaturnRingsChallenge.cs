using System;
using UnityEngine;
using UnityEngine.UI;
using Lumen.Core;
using Lumen.Data;
using Lumen.Gameplay.Effects;

namespace Lumen.Gameplay.Challenges
{
    /// <summary>
    /// Minigame de Saturno: atravessar uma fila de aneis luminosos pelo CENTRO. Se a nave passa
    /// longe demais do centro ela "encosta" no anel: o anel pisca em vermelho, toca o som de erro
    /// e a nave volta um pouco antes dele para tentar de novo (nunca perde a partida).
    /// Ao passar pelo ultimo anel chama onCompleted - o fragmento, a energia e a seta ficam a
    /// cargo do PlanetApproachTrigger, como no quiz.
    ///
    /// Leve para placa de video integrada: aneis feitos de sprites (SpriteFx), sem fisica, sem
    /// luzes e sem ParticleSystem. A passagem e detectada pela travessia do plano de cada anel.
    /// </summary>
    public class SaturnRingsChallenge : MonoBehaviour, IPhaseChallenge
    {
        private const int DotsPerRing = 28;

        // Deslocamento de cada anel em relacao ao eixo do corredor (direita, cima), em metros:
        // um zigue-zague suave que obriga a usar A/D e Espaco/Ctrl.
        private static readonly Vector2[] Offsets =
        {
            new Vector2(0f, 0f), new Vector2(20f, 9f), new Vector2(-18f, -7f),
            new Vector2(16f, 12f), new Vector2(-8f, 0f),
        };

        private static readonly Color Upcoming = new Color(0.45f, 0.85f, 1f, 0.55f);
        private static readonly Color Current = new Color(1f, 0.88f, 0.40f, 1f);
        private static readonly Color Passed = new Color(0.45f, 1f, 0.6f, 1f);
        private static readonly Color Hit = new Color(1f, 0.3f, 0.25f, 1f);

        private class Gate
        {
            public Transform Root;
            public SpriteRenderer[] Dots;
            public Vector3 Center;
            public float Flash;        // segundos restantes de piscada vermelha
            public float PassedFade;   // segundos restantes da animacao de "passou"
            public bool Done;
        }

        private Action _onCompleted;
        private Transform _planet;
        private Gate[] _gates;
        private Vector3 _axis;           // direcao do corredor (horizontal)
        private int _current;
        private bool _running;
        private Transform _ship;
        private LumenController _lumen;
        private LumenEnergySystem _energy;
        private Vector3 _prevPos;
        private float _messageTimer;
        private string _message = "";
        private Color _messageColor = Color.white;

        private Canvas _canvas;
        private Text _counter;
        private Text _messageText;

        /// <summary>O planeta: o corredor corre em volta dele (tangente), nunca para dentro dele.</summary>
        public void SetPlanet(Transform planet) => _planet = planet;

        public void StartChallenge(PhaseData phase, Action onCompleted)
        {
            _onCompleted = onCompleted;

            _ship = FindShip();
            if (_ship == null)
            {
                Debug.LogWarning("[LUMEN] Minigame de Saturno sem nave na cena - completando a fase.");
                onCompleted?.Invoke();
                return;
            }

            _lumen = _ship.GetComponent<LumenController>();
            _energy = _ship.GetComponent<LumenEnergySystem>();

            _axis = ChooseAxis();

            BuildGates();
            BuildUi();

            // A nave vira para o corredor (corte seco, como numa cena) e a camera acompanha.
            if (_lumen != null) _lumen.SetHeading(Mathf.Atan2(_axis.x, _axis.z) * Mathf.Rad2Deg);
            var follow = Camera.main != null ? Camera.main.GetComponent<Lumen.Cameras.CameraFollow>() : null;
            if (follow != null) follow.SnapToTarget();

            _current = 0;
            _prevPos = _ship.position;
            _running = true;
            _messageTimer = 0f;
            ShowMessage("Atravesse o centro de cada anel!", Color.white, 4f);
            RefreshColors();

            EventBus.SetMinigameActive(true);
            GameManager.Instance.SetState(GameState.Playing);
        }

        /// <summary>
        /// Direcao do corredor: perpendicular a linha nave->planeta (tangente), do lado para onde a
        /// nave ja esta mais virada. Sem planeta conhecido, usa o rumo atual da nave.
        /// </summary>
        private Vector3 ChooseAxis()
        {
            Vector3 fwd = _ship.forward;
            fwd.y = 0f;
            fwd = fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
            if (_planet == null) return fwd;

            Vector3 toPlanet = _planet.position - _ship.position;
            toPlanet.y = 0f;
            if (toPlanet.sqrMagnitude < 0.01f) return fwd;

            Vector3 tangent = Vector3.Cross(Vector3.up, toPlanet.normalized);
            return Vector3.Dot(tangent, fwd) >= 0f ? tangent : -tangent;
        }

        private static Transform FindShip()
        {
            var controller = UnityEngine.Object.FindFirstObjectByType<LumenController>();
            return controller == null ? null : controller.transform;
        }

        // ------------------------------------------------------------------ construcao

        private void BuildGates()
        {
            int count = GameTuning.SaturnGateCount;
            _gates = new Gate[count];
            Vector3 right = Vector3.Cross(Vector3.up, _axis);

            for (int i = 0; i < count; i++)
            {
                Vector2 off = Offsets[i % Offsets.Length];
                float along = GameTuning.SaturnFirstGateDistance + i * GameTuning.SaturnGateSpacing;
                Vector3 center = _ship.position + _axis * along + right * off.x + Vector3.up * off.y;

                var root = new GameObject("SaturnRing_" + i).transform;
                root.SetParent(transform, false);
                root.position = center;

                var gate = new Gate { Root = root, Center = center, Dots = new SpriteRenderer[DotsPerRing] };
                for (int d = 0; d < DotsPerRing; d++)
                {
                    float a = d / (float)DotsPerRing * Mathf.PI * 2f;
                    var dot = SpriteFx.Make("d", root, Upcoming, 3.4f, 6);
                    dot.transform.position = center + (right * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a)) * GameTuning.SaturnGateRadius;
                    gate.Dots[d] = dot;
                }
                _gates[i] = gate;
            }
        }

        private void BuildUi()
        {
            if (_canvas != null) { _canvas.gameObject.SetActive(true); return; }

            var go = new GameObject("SaturnRings_Canvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9;
            UiTextTuning.ConfigureScaler(go.AddComponent<CanvasScaler>());

            _counter = NewText(go.transform, "Counter", GameTuning.HudObjectiveFontSize + 6,
                new Vector2(0.36f, 0.90f), new Vector2(0.78f, 0.99f), new Color(1f, 0.88f, 0.40f));
            _messageText = NewText(go.transform, "Message", GameTuning.HudObjectiveFontSize,
                new Vector2(0.30f, 0.80f), new Vector2(0.84f, 0.90f), Color.white);
        }

        private static Text NewText(Transform parent, string name, int size, Vector2 min, Vector2 max, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(2f, -2f);
            var rt = t.rectTransform;
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return t;
        }

        // ------------------------------------------------------------------ jogo

        private void Update()
        {
            if (!_running) return;

            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentState != GameState.Playing)
            {
                if (_ship != null) _prevPos = _ship.position; // pausa: nao conta salto de posicao
                return;
            }

            KeepFuel();
            CheckGateCrossing();
            AnimateGates();
            UpdateTexts();

            if (_ship != null) _prevPos = _ship.position;
        }

        /// <summary>Durante o minigame o tanque nao zera: o resgate levaria a nave embora do corredor.</summary>
        private void KeepFuel()
        {
            if (_energy != null && _energy.CurrentEnergy < GameTuning.SaturnMinFuel)
                _energy.Refill(GameTuning.SaturnMinFuel - _energy.CurrentEnergy);
        }

        private void CheckGateCrossing()
        {
            if (_ship == null || _current >= _gates.Length) return;

            Gate gate = _gates[_current];
            Vector3 cur = _ship.position;
            float s0 = Vector3.Dot(_prevPos - gate.Center, _axis);
            float s1 = Vector3.Dot(cur - gate.Center, _axis);

            // Longe demais do corredor (o jogador se perdeu): volta para o anel atual.
            if (Vector3.Distance(cur, gate.Center) > GameTuning.SaturnLeashDistance)
            {
                Respawn(gate, "Volte para o corredor de anéis!");
                return;
            }

            if (!(s0 < 0f && s1 >= 0f)) return; // so conta a travessia para frente

            float u = Mathf.Approximately(s1, s0) ? 1f : -s0 / (s1 - s0);
            Vector3 atPlane = Vector3.Lerp(_prevPos, cur, u);
            float lateral = (atPlane - gate.Center).magnitude;

            if (lateral <= GameTuning.SaturnGateRadius - GameTuning.SaturnGateMargin)
                PassGate(gate);
            else
                HitGate(gate);
        }

        private void PassGate(Gate gate)
        {
            gate.Done = true;
            gate.PassedFade = 0.8f;
            _current++;
            EventBus.RaiseAnswerCorrect(); // som de acerto

            if (_current >= _gates.Length)
            {
                Finish();
                return;
            }

            ShowMessage("Muito bem! Próximo anel.", new Color(0.7f, 1f, 0.75f), 2f);
            RefreshColors();
        }

        private void HitGate(Gate gate)
        {
            gate.Flash = 0.7f;
            EventBus.RaiseAnswerIncorrect(); // som de erro
            Respawn(gate, "Encostou no anel! Tente de novo, sem pressa.");
        }

        /// <summary>Devolve a nave para tras do anel atual, parada e alinhada com o corredor.</summary>
        private void Respawn(Gate gate, string message)
        {
            Vector3 pos = gate.Center - _axis * GameTuning.SaturnRespawnBehind;
            _ship.position = pos;
            if (_lumen != null)
            {
                _lumen.StopMotion();
                _lumen.SetHeading(Mathf.Atan2(_axis.x, _axis.z) * Mathf.Rad2Deg);
            }
            var cam = Camera.main != null ? Camera.main.GetComponent<Lumen.Cameras.CameraFollow>() : null;
            if (cam != null) cam.SnapToTarget();

            _prevPos = pos;
            ShowMessage(message, Hit, 2.5f);
        }

        private void Finish()
        {
            _running = false;
            EventBus.SetMinigameActive(false);

            // Fica em Challenge: a nave espera parada enquanto o fragmento voa ate ela
            // (o PlanetApproachTrigger devolve o controle logo depois).
            GameManager.Instance.SetState(GameState.Challenge);
            if (_lumen != null) _lumen.StopMotion();

            if (_canvas != null) _canvas.gameObject.SetActive(false);

            // Os aneis passados somem logo, sem sobrar nada na cena.
            Invoke(nameof(CleanupGates), 1f);

            var done = _onCompleted;
            _onCompleted = null;
            done?.Invoke();
        }

        private void CleanupGates()
        {
            if (_gates == null || _running) return;
            foreach (var g in _gates)
                if (g != null && g.Root != null) Destroy(g.Root.gameObject);
            _gates = null;
        }

        // ------------------------------------------------------------------ visual

        private void ShowMessage(string text, Color color, float seconds)
        {
            _message = text;
            _messageColor = color;
            _messageTimer = seconds;
        }

        private void RefreshColors()
        {
            for (int i = 0; i < _gates.Length; i++)
            {
                if (_gates[i].Done) continue;
                SetDots(_gates[i], i == _current ? Current : Upcoming);
            }
        }

        private static void SetDots(Gate gate, Color color)
        {
            foreach (var d in gate.Dots) d.color = color;
        }

        private void AnimateGates()
        {
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 6f);
            for (int i = 0; i < _gates.Length; i++)
            {
                Gate g = _gates[i];
                bool active = g.Root != null;
                if (!active) continue;

                if (g.Done)
                {
                    g.PassedFade -= Time.deltaTime;
                    float a = Mathf.Clamp01(g.PassedFade / 0.8f);
                    var c = Passed; c.a = a;
                    SetDots(g, c);
                    if (g.PassedFade <= 0f) g.Root.gameObject.SetActive(false);
                }
                else if (g.Flash > 0f)
                {
                    g.Flash -= Time.deltaTime;
                    SetDots(g, Hit);
                    if (g.Flash <= 0f) RefreshColors();
                }

                if (!g.Root.gameObject.activeSelf) continue;
                float size = (i == _current && !g.Done) ? 3.4f * pulse : 3.4f;
                foreach (var d in g.Dots)
                {
                    SpriteFx.Billboard(d.transform);
                    SpriteFx.SetWorldDiameter(d.transform, size);
                }
            }
        }

        private void UpdateTexts()
        {
            if (_counter == null || _current >= _gates.Length) return;

            Gate gate = _gates[_current];
            float meters = Vector3.Distance(_ship.position, gate.Center);
            string hint = "";
            var cam = Camera.main;
            if (cam != null && Vector3.Dot(cam.transform.forward, gate.Center - cam.transform.position) < 0f)
                hint = "  (vire-se)";
            _counter.text = $"ANEL {_current + 1}/{_gates.Length}  ·  {meters:F0} m{hint}";

            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.deltaTime;
                _messageText.text = _message;
                _messageText.color = _messageColor;
            }
            else if (_messageText.text.Length > 0)
            {
                _messageText.text = "";
            }
        }

        private void OnDisable()
        {
            if (_running) EventBus.SetMinigameActive(false);
        }
    }
}
