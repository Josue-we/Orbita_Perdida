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
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Musica")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        [Header("Efeitos sonoros")]
        [SerializeField] private AudioClip selectSfx;
        [SerializeField] private AudioClip correctSfx;
        [SerializeField] private AudioClip incorrectSfx;
        [SerializeField] private AudioClip collectSfx;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

        private AudioSource _musicSource;
        private AudioSource _sfxSource;

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
        }

        private void OnDisable()
        {
            EventBus.OnOptionSelected -= HandleOptionSelected;
            EventBus.OnAnswerCorrect -= HandleAnswerCorrect;
            EventBus.OnAnswerIncorrect -= HandleAnswerIncorrect;
            EventBus.OnFragmentCollected -= HandleFragmentCollected;
        }

        private void Start()
        {
            if (backgroundMusic == null)
            {
                Debug.LogWarning("[LUMEN] Nenhuma trilha atribuida no AudioManager. " +
                                  "Arraste o arquivo de musica para o campo 'Background Music' no Inspector.");
                return;
            }

            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }

        private void HandleOptionSelected() => PlaySfx(selectSfx);
        private void HandleAnswerCorrect() => PlaySfx(correctSfx);
        private void HandleAnswerIncorrect() => PlaySfx(incorrectSfx);
        private void HandleFragmentCollected(string planetName) => PlaySfx(collectSfx);

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            _musicSource.volume = musicVolume;
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
