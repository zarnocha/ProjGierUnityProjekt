using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayMusicThroughScenes : MonoBehaviour
{
    private AudioSource audioSource;
    private bool isPlaying = false;
    public static float timestamp = 0f;
    public float volume;

    private void Awake()
    {
        DontDestroyOnLoad(transform.gameObject);
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isPlaying && audioSource.time != 0)
        {
            timestamp = audioSource.time;
        }
    }

    public void PlayMusic()
    {
        isPlaying = true;
        audioSource.time = timestamp;
        float playerVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
        ChangeVolume(playerVolume);
        if (audioSource.isPlaying) return;
        audioSource.Play();
    }

    public void StopMusic()
    {
        isPlaying = false;
        audioSource.Stop();
    }

    public void ChangeVolume(float newVolume)
    {
        audioSource.volume = newVolume;
    }
}
