using UnityEngine;

namespace Broniek.Stuff.Sounds
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private AudioClip[] backgrounds;
        [SerializeField] private AudioClip[] effects;

        private static AudioClip[] backgroundSounds;
        private static AudioClip[] soundEffects;

        private static AudioSource aSource;

        private void Start()
        {
            soundEffects = effects;
            backgroundSounds = backgrounds;

            aSource = GetComponent<AudioSource>();
        }

        public static void SetBackgroundSound(int index, float volume, float delay = 0)
        {
            aSource.clip = backgroundSounds[index % backgroundSounds.Length];
            aSource.volume = volume;
            aSource.PlayDelayed(delay);
        }

        public static void GetSoundEffect(AudioClip clip, float volume, float delay = 0)
        {
            CreateSound(clip, volume, delay);
        }

        public static void GetSoundEffect(int index, float volume, float delay = 0)
        {
            CreateSound(soundEffects[index], volume, delay);
        }

        private static void CreateSound(AudioClip clip, float volume, float delay = 0)
        {
            GameObject go = new GameObject("SoundObject", typeof(AudioSource));
            AudioSource audioSource = go.GetComponent<AudioSource>();
            DontDestroyOnLoad(go);
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.PlayDelayed(delay);
            Destroy(go, delay + clip.length);   // można zastosować Pool Object System
        }

        public static void PlayFixed(float volume, float delay = 0)
        {
            aSource.volume = volume;
            aSource.PlayDelayed(delay);
        }
    }
}