using R3;
using UnityEngine;

/// <summary>
/// Drives the microwave's own sounds from MicrowaveTimer's state and the dish
/// that MovingInsideSystem parents inside the cavity. Observes public API only,
/// so the heating and dish-moving logic stay untouched. Sits on the same
/// GameObject as MicrowaveTimer.
/// </summary>
public class MicrowaveSound : MonoBehaviour
{
    [SerializeField] private AudioSource humSource;
    [SerializeField] private AudioSource endSource;
    [SerializeField] private AudioSource dishSource;
    [SerializeField] private AudioSource tickSource;
    [SerializeField] private AudioSource plateSource;

    [SerializeField] private AudioClip humClip;
    [SerializeField] private AudioClip endClip;
    [SerializeField] private AudioClip dishClip;
    [SerializeField] private AudioClip tickClip;
    [SerializeField] private AudioClip plateClip;

    [SerializeField, Range(0f, 1f)] private float humVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float endVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float dishVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float tickVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float plateVolume = 1f;

    [SerializeField,
     Tooltip("Delay from the dish entering the cavity to it settling, so the sound lands on the landing")]
    private float plateSettleDelay = 1f;

    private MicrowaveTimer _timer;
    private MicrowaveState _previous;
    private Dish _dish;
    private float _lastTimerValue;
    private bool _platePlayed;
    private float _plateDueAt = -1f;

    private void Awake()
    {
        _timer = GetComponent<MicrowaveTimer>();
        _previous = _timer.State;
        _lastTimerValue = _timer.Timer;

        Configure(humSource, humClip, true, humVolume);
        Configure(dishSource, dishClip, true, dishVolume);
        Configure(endSource, null, false, endVolume);
        Configure(tickSource, null, false, tickVolume);
        Configure(plateSource, null, false, plateVolume);

        // The countdown is the only thing that lowers the timer; adding time
        // raises it, so comparing against the previous value keeps the tick off
        // the time buttons.
        _timer.onTimerChanged
            .Subscribe(OnTimerChanged)
            .AddTo(this);
    }

    private static void Configure(AudioSource source, AudioClip clip, bool loop, float volume)
    {
        if (source == null) return;

        if (clip != null) source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.volume = volume;
    }

    private void OnTimerChanged(float value)
    {
        if (value < _lastTimerValue && tickSource != null && tickClip != null)
        {
            tickSource.PlayOneShot(tickClip, tickVolume);
        }

        _lastTimerValue = value;
    }

    private void Update()
    {
        var state = _timer.State;

        if (state != _previous)
        {
            // The motor runs only while heating, and cuts the moment it stops -
            // matching how a real microwave behaves on pause, stop or finish.
            if (state == MicrowaveState.Heating)
            {
                StartHum();
            }
            else if (_previous == MicrowaveState.Heating)
            {
                StopHum();
            }

            if (state == MicrowaveState.Finished)
            {
                PlayEndChime();
            }

            _previous = state;
        }

        TrackDish();
        UpdateDishLayer(state);
    }

    /// <summary>
    /// Tracks the dish currently inside the cavity. MovingInsideSystem reparents
    /// the dish under the microwave when it goes in and back out again when it
    /// leaves, so a child Dish appearing means one was just loaded. Re-arming on
    /// every entry (not just the first) makes the plate-landing sound play each
    /// time a dish goes in - including re-inserting the same dish after it exploded.
    /// </summary>
    private void TrackDish()
    {
        var current = GetComponentInChildren<Dish>();
        if (current == _dish) return;

        _dish = current;

        if (_dish != null)
        {
            // The dish is reparented as its travel starts, so wait for it to
            // settle before the plate lands.
            _platePlayed = false;
            _plateDueAt = Time.time + plateSettleDelay;
        }
        else
        {
            _plateDueAt = -1f;
        }
    }

    private void UpdateDishLayer(MicrowaveState state)
    {
        if (!_platePlayed && _plateDueAt >= 0f && Time.time >= _plateDueAt)
        {
            PlatePlaced();
        }

        if (dishSource == null || dishClip == null) return;

        var cooking = state == MicrowaveState.Heating
                      && _dish != null
                      && _dish.DishStatus == DishStatus.InProgress;

        if (cooking && !dishSource.isPlaying)
        {
            dishSource.Play();
        }
        else if (!cooking && dishSource.isPlaying)
        {
            dishSource.Stop();
        }
    }

    private void PlatePlaced()
    {
        _platePlayed = true;

        if (plateSource == null || plateClip == null) return;

        plateSource.PlayOneShot(plateClip, plateVolume);
    }

    private void StartHum()
    {
        if (humSource == null || humClip == null || humSource.isPlaying) return;

        humSource.Play();
    }

    private void StopHum()
    {
        if (humSource == null) return;

        humSource.Stop();
    }

    private void PlayEndChime()
    {
        if (endSource == null || endClip == null) return;

        endSource.PlayOneShot(endClip, endVolume);
    }
}
