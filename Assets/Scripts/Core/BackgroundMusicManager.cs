using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicManager : MonoBehaviour
{
    private static BackgroundMusicManager instance;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            Destroy(gameObject);
            return;
        }

        if (instance == null)
        {
            instance = this;

            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            audioSource.loop = true;

            if (!audioSource.isPlaying)
                audioSource.Play();

            return;
        }

        AudioSource existingSource = instance.GetComponent<AudioSource>();

        bool sameMusic =
            existingSource != null &&
            existingSource.clip == audioSource.clip;

        if (sameMusic)
        {
            Destroy(gameObject);
            return;
        }

        if (existingSource != null)
        {
            existingSource.clip = audioSource.clip;
            existingSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            existingSource.loop = true;
            existingSource.Play();
        }

        Destroy(gameObject);
    }
}