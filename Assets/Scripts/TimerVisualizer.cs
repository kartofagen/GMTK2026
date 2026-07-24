using System;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

public class TimerVisualizer : MonoBehaviour
{
    private TextMeshProUGUI _timerText;
    [SerializeField] private MicrowaveTimer microwave;
    
    private void Awake()
    {
        _timerText = GetComponent<TextMeshProUGUI>();
        
        microwave
            .onTimerChanged
            .Subscribe(OnTimerChanged)
            .AddTo(this);
        
        microwave
            .onFinished
            .Subscribe(OnFinished)
            .AddTo(this);
    }
    
    private void OnTimerChanged(float time)
    {
        _timerText.SetText(TimeSpan.FromSeconds(time).ToString(@"mm\:ss"));
        _timerText.ForceMeshUpdate();
    }
    
    private void OnFinished(Unit unit)
    {
        Sequence intro = DOTween.Sequence();
        intro.AppendInterval(0.1f).AppendCallback(Flashing);
    }

    private void Flashing()
    {
        Sequence loop = DOTween.Sequence();

        loop.AppendInterval(0.5f)
            .AppendCallback(() => _timerText.enabled = false)
            .AppendInterval(0.5f)
            .AppendCallback(() => _timerText.enabled = true)
            .SetLoops(3);
    }
}
