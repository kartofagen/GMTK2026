using DG.Tweening;
using R3;
using UnityEngine;

/// <summary>
/// Plays a plate-break sound when a failed dish is taken out to be thrown away -
/// overheated, underheated or exploded, i.e. anything but a clean success. The
/// final result is captured when heating finishes and the sound fires when the
/// dish actually leaves the cavity. Pitch is jittered so repeats sound different.
/// Observes the microwave's public events, so nothing else has to change.
/// </summary>
public class PlateBreakSound : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    private HeatingSystem _heating;
    private MovingInsideSystem _moving;
    private DishStatus _lastStatus = DishStatus.InProgress;
    
    private Tween _delayTween;

    private void Awake()
    {
        _heating = GetComponent<HeatingSystem>();
        _moving = GetComponent<MovingInsideSystem>();

        if (source != null)
        {
            source.playOnAwake = false;
        }

        _heating.onHeatingFinished
            .Subscribe(status => _lastStatus = status)
            .AddTo(this);

        _moving.onMovingOutside
            .Subscribe(_ => OnMovingOutside())
            .AddTo(this);
    }

    private void OnMovingOutside()
    {
        var thrownAway = _lastStatus == DishStatus.Underheating
                         || _lastStatus == DishStatus.Overheating
                         || _lastStatus == DishStatus.Exploded
                         || _lastStatus == DishStatus.InProgress;

        var delay = 1f;
        if (_lastStatus == DishStatus.Overheating)
        {
            delay = 2f;
        }
        else if (_lastStatus == DishStatus.Exploded)
        {
            delay = 0.5f;
        }

        // Reset so a later exit without a fresh result can't replay the sound.
        _lastStatus = DishStatus.InProgress;

        if (!thrownAway || source == null || clips == null || clips.Length == 0) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        
        _delayTween?.Kill();
        _delayTween = DOVirtual.DelayedCall(delay, () =>
        {
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
        });
    }

    private void OnDestroy()
    {
        _delayTween?.Kill();
    }
}
