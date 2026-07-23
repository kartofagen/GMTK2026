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
    public float timer = 0f;
    public MicrowaveState State { get; set; } = MicrowaveState.Idle;

    public readonly Subject<float> onTimerChanged = new();
    private CompositeDisposable _tickSubscription = new();

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
        if (timer > 0f)
        {
            timer -= 1f;
            onTimerChanged.OnNext(timer);
        }
        else
        {
            FinishHeating();
        }
    }
    
    private void StopTicks() => _tickSubscription.Dispose();

    public void AddTime(float time)
    {
        timer += time;
        onTimerChanged.OnNext(timer);
        
        StopTicks();
        StartHeating();
    }

    public void PauseHeating()
    {
        StopTicks();
        State = MicrowaveState.Paused;
    }

    private void FinishHeating()
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
