using System;
using R3;
using UnityEngine;

public enum MicrowaveState
{
    Idle,
    Heating,
    Paused,
    Finished
}

public class MicrowaveTimer : MonoBehaviour
{
    private float _timer;
    
    public MicrowaveState State { get; private set; } = MicrowaveState.Idle;

    public readonly Subject<float> onTimerChanged = new();
    private CompositeDisposable _tickSubscription = new();

    public float Timer => _timer;
    
    private void StartHeating()
    {
        _tickSubscription = new CompositeDisposable();
        
        Observable
            .Interval(TimeSpan.FromSeconds(1f), UnityTimeProvider.Update)
            .Subscribe(_ => Tick())
            .AddTo(_tickSubscription);

        State = MicrowaveState.Heating;
    }

    private void Tick()
    {
        if (_timer > 0f)
        {
            _timer -= 1f;
            onTimerChanged.OnNext(_timer);
        }
        else
        {
            FinishHeating();
        }
    }
    
    private void StopTicks() => _tickSubscription.Dispose();

    public void AddTime(float time)
    {
        _timer += time;
        onTimerChanged.OnNext(_timer);
        
        StopTicks();
        StartHeating();
    }

    public void PauseHeating()
    {
        StopTicks();
        State = MicrowaveState.Paused;
    }

    public void FinishHeating()
    {
        StopTicks();
        State = MicrowaveState.Finished;
    }
    
    public void SetPower(float power)
    {
        
    }

    private void OnDestroy()
    {
        _tickSubscription.Dispose();
    }
}
