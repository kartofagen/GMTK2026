using System;
using R3;
using TMPro;
using UnityEngine;

public class TimerVisualizer : MonoBehaviour
{
    private TextMeshProUGUI _timerText;
    [SerializeField] private Microwave microwave;
    
    private void Awake()
    {
        _timerText = GetComponent<TextMeshProUGUI>();
        
        microwave
            .OnTimerChanged
            .Subscribe(OnTimerChanged)
            .AddTo(this);
    }
    
    private void OnTimerChanged(float time)
    {
        _timerText.SetText(TimeSpan.FromSeconds(time).ToString(@"mm\:ss"));
        _timerText.ForceMeshUpdate();
    }
}
