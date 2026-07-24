using UnityEngine;

/// <summary>
/// Drives the running hum and the end chime from MicrowaveTimer's state.
/// Polls the public State property rather than hooking into the timer, so the
/// heating logic stays untouched. Sits on the same GameObject as the timer.
/// </summary>
public class MicrowaveSound : MonoBehaviour
{
    [SerializeField] private AudioSource humSource;
    [SerializeField] private AudioSource endSource;
    [SerializeField] private AudioSource dishSource;
    [SerializeField] private AudioClip humClip;
    [SerializeField] private AudioClip endClip;
    [SerializeField] private AudioClip dishClip;
    [SerializeField, Range(0f, 1f)] private float humVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float endVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float dishVolume = 1f;

    private MicrowaveTimer _timer;
    private MicrowaveState _previous;
    private Dish _dish;

    private void Awake()
    {
        _timer = GetComponent<MicrowaveTimer>();
        _previous = _timer.State;

        if (humSource != null)
        {
            humSource.clip = humClip;
            humSource.loop = true;
            humSource.playOnAwake = false;
            humSource.volume = humVolume;
        }

        if (endSource != null)
        {
            endSource.playOnAwake = false;
        }

        if (dishSource != null)
        {
            dishSource.clip = dishClip;
            dishSource.loop = true;
            dishSource.playOnAwake = false;
            dishSource.volume = dishVolume;
        }

        // Nothing spawns or removes dishes at runtime, so the one wired up in the
        // scene is the one that is in the microwave for the whole session.
        _dish = FindAnyObjectByType<Dish>();
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

        UpdateDishLayer(state);
    }

    /// <summary>
    /// Layers the food's own sound over the hum while it is actually cooking.
    /// Checked every frame rather than on state changes, because the dish can
    /// finish or explode part way through a run.
    /// </summary>
    private void UpdateDishLayer(MicrowaveState state)
    {
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
