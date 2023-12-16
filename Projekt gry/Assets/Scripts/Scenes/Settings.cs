using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Slider volume;
    public Slider sensitivity;

    private float oldVolumeValue;
    private float oldSensitivityValue;

    private float newVolumeValue;
    public float newSensitivityValue;

    void Start()
    {
        float playerVolume = PlayerPrefs.GetFloat("Volume", 0.5f);
        float playerSensitivity = PlayerPrefs.GetFloat("Sensitivity", 200f);

        volume.value = playerVolume;
        sensitivity.value = playerSensitivity;

        oldVolumeValue = playerVolume;
        oldSensitivityValue = playerSensitivity;
    }

    public void OnVolumeChange() 
    {
        newVolumeValue = volume.value;
        if (GameObject.FindWithTag("Music"))
        {
            GameObject.FindWithTag("Music").GetComponent<PlayMusicThroughScenes>().ChangeVolume(newVolumeValue);
        }
    }
    public void OnSensitivityChange()
    {
        newSensitivityValue = sensitivity.value;
    }

    public void OnSave()
    {
        PlayerPrefs.SetFloat("Volume", newVolumeValue);
        PlayerPrefs.SetFloat("Sensitivity", newSensitivityValue);
        oldVolumeValue = newVolumeValue; 
        oldSensitivityValue = newSensitivityValue;
    }

    public void OnExit()
    {
        Scene gameScene = SceneManager.GetSceneByBuildIndex(2);

        if (gameScene.isLoaded)
        {
            SceneManager.SetActiveScene(gameScene);
        } else
        {
            SceneManager.LoadScene(0);
        }

        PlayerPrefs.SetFloat("Volume", oldVolumeValue);
        PlayerPrefs.SetFloat("Sensitivity", oldSensitivityValue);
        SceneManager.LoadScene(0);
    }
}
