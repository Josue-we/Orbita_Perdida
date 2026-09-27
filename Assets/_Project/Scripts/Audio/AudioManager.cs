using System.Collections;
using UnityEngine;
using Lumen.Core;

namespace Lumen.Audio
{
    /// <summary>
    /// Toca a trilha de fundo em loop e os efeitos sonoros de jogo (selecao,
    /// acerto, erro, coleta de fragmento), reagindo aos eventos do EventBus.
    /// Nenhum outro sistema precisa conhecer o AudioManager diretamente - o
    /// futuro sistema de quiz so precisa chamar EventBus.RaiseAnswerCorrect()
    /// e o som ja toca sozinho.
    ///
    /// Com o quiz em tela a trilha abaixa (ducking) para a leitura ficar
    /// confortavel, e volta EXATAMENTE ao volume anterior quando o quiz fecha:
    /// o volume final e sempre musicVolume * fator, entao desfazer o duck
    /// devolve o que o jogador/escenario tinha escolhido.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Música")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

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
        }

        private void OnDisable()
        {
            EventBus.OnOptionSelected -= HandleOptionSelected;
            EventBus.OnAnswerCorrect -= HandleAnswerCorrect;
            EventBus.OnAnswerIncorrect -= HandleAnswerIncorrect;
            EventBus.OnFragmentCollected -= HandleFragmentCollected;
            EventBus.OnQuizOpened -= HandleQuizOpened;
            EventBus.OnQuizClosed -= HandleQuizClosed;

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
            if (backgroundMusic == null)
            {
                Debug.LogWarning("[LUMEN] Nenhuma trilha atribuída no AudioManager. " +
                                  "Arraste o arquivo de música para o campo 'Background Music' no Inspector.");
                return;
            }

            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }

        private void HandleOptionSelected() => PlaySfx(selectSfx);
        private void HandleAnswerCorrect() => PlaySfx(correctSfx);
        private void HandleAnswerIncorrect() => PlaySfx(incorrectSfx);
        private void HandleFragmentCollected(string planetName) => PlaySfx(collectSfx);

        private void HandleQuizOpened() => FadeMusicTo(quizMusicMultiplier, duckFadeSeconds);
        private void HandleQuizClosed() => FadeMusicTo(1f, duckFadeSeconds);

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

        public bool HasMusicClip => backgroundMusic != null;
        public void SetMusicClip(AudioClip clip) => backgroundMusic = clip;

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
