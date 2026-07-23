using System;
using R3;
using UnityEngine;

/*public enum DishResult
{
    InProgress,
    Success,
    Underheated,
    Exploded
}

public enum MicrowaveState
{
    Idle,
    Heating,
    Paused,
    CoolingDown,
    StopOnCooldown
}*/

public class Microwave : MonoBehaviour
{
    public float timer = 0f;
    
    public Vector2 targetTempRange = new Vector2(65f, 75f);
    public float startTemp = 5f;
    public float explosionThreshold = 100f;
    
    [Header("Cooling")]
    public AnimationCurve coolingCurve;
    public float roomTemp = 20f;
    public float coolingToAverageSpeed = 1f;
    
    private bool _isHeating = false;
    
    public bool IsStopAvailable { get; set; }
    
    public readonly Subject<float> OnTimerChanged = new();
    
    private CompositeDisposable _tickSubscription = new();

    private void StartHeating()
    {
        _tickSubscription = new CompositeDisposable();
        
        Observable
            .Interval(TimeSpan.FromSeconds(1f), UnityTimeProvider.Update)
            .Subscribe(_ => Tick())
            .AddTo(_tickSubscription);

        _isHeating = true;
    }

    private void Tick()
    {
        if (timer > 0f)
        {
            timer -= 1f;
            OnTimerChanged.OnNext(timer);
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
        OnTimerChanged.OnNext(timer);
        
        StopTicks();
        StartHeating();
    }

    public void PauseHeating()
    {
        StopTicks();
    }

    private void FinishHeating()
    {
        StopTicks();
        _isHeating = false;
    }
    
    public void SetPower(float powerLevel)
    {
        
    }

    private void OnDestroy()
    {
        _tickSubscription.Dispose();
    }
}
