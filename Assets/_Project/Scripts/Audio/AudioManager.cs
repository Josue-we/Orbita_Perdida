using System.Collections;
using UnityEngine;
using Lumen.Core;

namespace Lumen.Audio
{
    /// <summary>
    /// Toca a trilha de fundo e os efeitos sonoros de jogo (selecao, acerto, erro,
    /// coleta de fragmento), reagindo aos eventos do EventBus. Nenhum outro sistema
    /// precisa conhecer o AudioManager diretamente.
    ///
    /// DUAS trilhas separadas: "Menu Music" toca na tela inicial (assim que a cena
    /// carrega) e "Gameplay Music" entra com um crossfade suave assim que o
    /// jogador clica em "Jogar" (EventBus.OnGameStarted). Se uma das duas nao
    /// estiver atribuida, o AudioManager usa a outra como fallback em vez de
    /// ficar mudo.
    ///
    /// Com o quiz em tela a trilha (a que estiver tocando) abaixa (ducking) para
    /// a leitura ficar confortavel, e volta EXATAMENTE ao volume anterior quando
    /// o quiz fecha.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Música da tela inicial")]
        [SerializeField] private AudioClip menuMusic;

        [Header("Música da gameplay")]
        [SerializeField] private AudioClip gameplayMusic;

        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("Transição menu -> jogo")]
        [Tooltip("Segundos do crossfade ao trocar da música do menu para a da gameplay.")]
        [SerializeField] private float musicTransitionSeconds = 0.6f;

        [Header("Música durante o quiz")]
        [Tooltip("Multiplicador aplicado na trilha enquanto o quiz está em tela.")]
        [SerializeField, Range(0f, 1f)] private float quizMusicMultiplier = 0.25f;
        [Tooltip("Segundos da transição suave de volume (entrar e sair do quiz).")]
        [SerializeField] private float duckFadeSeconds = 0.35f;

        [Header("Efeitos sonoros")]
        [SerializeField] private AudioClip selectSfx;
        [SerializeField] private AudioClip correctSfx;
        [SerializeField] private AudioClip incorrectSfx;
        [SerializeField] private AudioClip collectSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private float _duckFactor = 1f;
        private Coroutine _duckRoutine;
        private Coroutine _transitionRoutine;

        private void Awake()
        {
            var sources = GetComponents<AudioSource>();
            _musicSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
            _sfxSource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;
            _musicSource.volume = musicVolume;

            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            EventBus.OnOptionSelected += HandleOptionSelected;
            EventBus.OnAnswerCorrect += HandleAnswerCorrect;
            EventBus.OnAnswerIncorrect += HandleAnswerIncorrect;
            EventBus.OnFragmentCollected += HandleFragmentCollected;
            EventBus.OnQuizOpened += HandleQuizOpened;
            EventBus.OnQuizClosed += HandleQuizClosed;
            EventBus.OnGameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            EventBus.OnOptionSelected -= HandleOptionSelected;
            EventBus.OnAnswerCorrect -= HandleAnswerCorrect;
            EventBus.OnAnswerIncorrect -= HandleAnswerIncorrect;
            EventBus.OnFragmentCollected -= HandleFragmentCollected;
            EventBus.OnQuizOpened -= HandleQuizOpened;
            EventBus.OnQuizClosed -= HandleQuizClosed;
            EventBus.OnGameStarted -= HandleGameStarted;

            // Sai do quiz com o painel fechado: garante a trilha em volume cheia.
            if (_duckRoutine != null)
            {
                StopCoroutine(_duckRoutine);
                _duckRoutine = null;
            }

            _duckFactor = 1f;
            if (_musicSource != null) _musicSource.volume = musicVolume;
        }

        private void Start()
        {
            // A tela inicial e sempre a primeira coisa que o jogador ve, entao a
            // musica do menu comeca aqui. Se ela nao estiver atribuida, cai para
            // a musica da gameplay (para nunca ficar mudo por engano).
            AudioClip initialClip = menuMusic != null ? menuMusic : gameplayMusic;

            if (initialClip == null)
            {
                Debug.LogWarning("[LUMEN] Nenhuma trilha atribuída no AudioManager (nem menu, nem gameplay). " +
                                  "Arraste os arquivos de música para os campos 'Menu Music' e 'Gameplay Music' no Inspector.");
                return;
            }

            _musicSource.clip = initialClip;
            _musicSource.Play();
        }

        private void HandleOptionSelected() => PlaySfx(selectSfx);
        private void HandleAnswerCorrect() => PlaySfx(correctSfx);
        private void HandleAnswerIncorrect() => PlaySfx(incorrectSfx);
        private void HandleFragmentCollected(string planetName) => PlaySfx(collectSfx);

        private void HandleQuizOpened() => FadeMusicTo(quizMusicMultiplier, duckFadeSeconds);
        private void HandleQuizClosed() => FadeMusicTo(1f, duckFadeSeconds);

        /// <summary>Jogador clicou "Jogar": troca para a trilha da gameplay com um crossfade curto.</summary>
        private void HandleGameStarted()
        {
            if (gameplayMusic == null) return; // sem trilha de gameplay, mantem a que ja esta tocando
            if (_musicSource.clip == gameplayMusic) return; // ja e a trilha atual, nada a fazer

            if (_duckRoutine != null)
            {
                StopCoroutine(_duckRoutine);
                _duckRoutine = null;
            }
            _duckFactor = 1f;

            if (_transitionRoutine != null) StopCoroutine(_transitionRoutine);
            _transitionRoutine = StartCoroutine(TransitionMusicRoutine(gameplayMusic));
        }

        private IEnumerator TransitionMusicRoutine(AudioClip newClip)
        {
            float seconds = Mathf.Max(0.01f, musicTransitionSeconds);

            // Some (fade-out) a trilha atual. Unscaled: funciona mesmo com o jogo pausado.
            float startVolume = _musicSource.volume;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / seconds);
                yield return null;
            }

            _musicSource.Stop();
            _musicSource.clip = newClip;
            _musicSource.Play();

            // Sobe (fade-in) a nova trilha ate o volume normal.
            float targetVolume = musicVolume * _duckFactor;
            elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / seconds);
                yield return null;
            }

            _musicSource.volume = targetVolume;
            _transitionRoutine = null;
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        /// <summary>Move o volume da trilha para musicVolume * alvo, sem mexer nos efeitos.</summary>
        private void FadeMusicTo(float target, float seconds)
        {
            if (_musicSource == null) return;
            if (_duckRoutine != null) StopCoroutine(_duckRoutine);
            _duckRoutine = StartCoroutine(FadeMusicRoutine(target, seconds));
        }

        private IEnumerator FadeMusicRoutine(float target, float seconds)
        {
            float startFactor = _duckFactor;
            // Unscaled: a transicao tambem funciona com o jogo parado/pausado.
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _duckFactor = Mathf.Lerp(startFactor, target, seconds > 0f ? elapsed / seconds : 1f);
                _musicSource.volume = musicVolume * _duckFactor;
                yield return null;
            }

            _duckFactor = target;
            _musicSource.volume = musicVolume * _duckFactor;
            _duckRoutine = null;
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (_musicSource != null) _musicSource.volume = musicVolume * _duckFactor;
        }

        public bool HasMenuMusicClip => menuMusic != null;
        public void SetMenuMusicClip(AudioClip clip) => menuMusic = clip;

        public bool HasGameplayMusicClip => gameplayMusic != null;
        public void SetGameplayMusicClip(AudioClip clip) => gameplayMusic = clip;

        public bool HasSelectSfx => selectSfx != null;
        public void SetSelectSfx(AudioClip clip) => selectSfx = clip;

        public bool HasCorrectSfx => correctSfx != null;
        public void SetCorrectSfx(AudioClip clip) => correctSfx = clip;

        public bool HasIncorrectSfx => incorrectSfx != null;
        public void SetIncorrectSfx(AudioClip clip) => incorrectSfx = clip;

        public bool HasCollectSfx => collectSfx != null;
        public void SetCollectSfx(AudioClip clip) => collectSfx = clip;
    }
}
