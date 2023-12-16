using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReloadingSlider : MonoBehaviour
{
    public float fillSpeed = 0.5f;

    private Slider reloadingSlider;
    private float targetProgress = 0;

    private void Awake()
    {
        reloadingSlider = GetComponent<Slider>();
    }

    void Update()
    {
        if (reloadingSlider.value < targetProgress)
        {
            reloadingSlider.value += fillSpeed * Time.deltaTime;
        }
    }

    public void IncrementProgress(float progress)
    {
        if (reloadingSlider != null)
        {
            targetProgress = reloadingSlider.value + progress;
        }
    }

    public void SetProgressToZero()
    {
        targetProgress = 0;
        reloadingSlider.value = 0;
    }

    public void ToggleSliderVisibility(bool isVisible)
    {
        reloadingSlider.gameObject.SetActive(isVisible);
    }
}
