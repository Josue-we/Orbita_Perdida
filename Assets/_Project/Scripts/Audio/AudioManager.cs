using UnityEngine;

namespace Lumen.Audio
{
    /// <summary>
    /// Toca a trilha de fundo em loop. Depois de importar o arquivo exportado
    /// do Suno para Assets/_Project/Audio/Music, arraste-o para o campo
    /// "Background Music" deste componente no Inspector.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;

        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D - musica de fundo, nao posicional
            _source.volume = musicVolume;
        }

        private void Start()
        {
            if (backgroundMusic == null)
            {
                Debug.LogWarning("[LUMEN] Nenhuma trilha atribuida no AudioManager. " +
                                  "Arraste o arquivo de musica para o campo 'Background Music' no Inspector.");
                return;
            }

            _source.clip = backgroundMusic;
            _source.Play();
        }

        public void SetVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            _source.volume = musicVolume;
        }
    }
}
