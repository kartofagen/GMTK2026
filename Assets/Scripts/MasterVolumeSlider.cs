using UnityEngine;
using UnityEngine.UI;

public class MasterVolumeSlider : MonoBehaviour
{
    [SerializeField] private Slider slider;

    private void Start()
    {
        slider.value = AudioListener.volume;
        slider.onValueChanged.AddListener(v => AudioListener.volume = v);
    }
}
