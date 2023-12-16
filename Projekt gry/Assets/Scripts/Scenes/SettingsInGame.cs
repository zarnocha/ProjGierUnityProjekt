using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsInGame : MonoBehaviour
{
    public GameObject pauseMenu;

    public Slider volume;
    public Slider sensitivity;

    private PlayerMove playerMoveScript;

    private float oldVolumeValue;
    private float oldSensitivityValue;

    private float newVolumeValue;
    public float newSensitivityValue;

    void Start()
    {
        gameObject.SetActive(false);

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
        playerMoveScript = FindObjectOfType<PlayerMove>();

        if (!playerMoveScript.IsUnityNull())
        {
            playerMoveScript.sensitivity = newSensitivityValue;
        }

        PlayerPrefs.SetFloat("Volume", newVolumeValue);
        PlayerPrefs.SetFloat("Sensitivity", newSensitivityValue);
        oldVolumeValue = newVolumeValue; 
        oldSensitivityValue = newSensitivityValue;
    }

    public void OnExit()
    {
        PlayerPrefs.SetFloat("Volume", oldVolumeValue);
        PlayerPrefs.SetFloat("Sensitivity", oldSensitivityValue);
        gameObject.SetActive(false);
        pauseMenu.SetActive(true);
    }
}
