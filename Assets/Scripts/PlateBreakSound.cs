using R3;
using UnityEngine;

/// <summary>
/// Plays a reaction sound when a dish is taken out of the microwave, chosen by
/// the final result:
///  - overheated: an "ouch" as the hot plate is grabbed, then a plate break as
///    it is thrown away;
///  - underheated / exploded: a plate break (thrown away);
///  - success: a "tasty" reaction.
/// The result is captured when heating finishes and the sound fires when the
/// dish actually leaves the cavity. Pitch is jittered so repeats sound
/// different. Observes the microwave's public events, so nothing else changes.
/// </summary>
public class PlateBreakSound : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField, Tooltip("Thrown-away dishes (underheated, exploded)")]
    private AudioClip[] clips;
    [SerializeField, Tooltip("Overheated: the ouch when the hot plate is grabbed")]
    private AudioClip[] overheatClips;
    [SerializeField, Tooltip("Successful dishes - tasty!")]
    private AudioClip[] successClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);
    [SerializeField, Tooltip("Delay before the plate break, after the ouch, on overheated dishes")]
    private float breakDelayAfterOuch = 0.6f;

    private HeatingSystem _heating;
    private MovingInsideSystem _moving;
    private DishStatus _lastStatus = DishStatus.InProgress;

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
        var status = _lastStatus;

        // Reset so a later exit without a fresh result can't replay the sound.
        _lastStatus = DishStatus.InProgress;

        switch (status)
        {
            case DishStatus.Overheating:
                // Grabbing the hot plate hurts first, then it gets thrown away.
                Play(overheatClips);
                if (HasClips(clips))
                {
                    Invoke(nameof(PlayBreakDelayed), breakDelayAfterOuch);
                }
                break;
            case DishStatus.Underheating:
            case DishStatus.Exploded:
                Play(clips);
                break;
            case DishStatus.Success:
                Play(successClips);
                break;
        }
    }

    private void PlayBreakDelayed() => Play(clips);

    private static bool HasClips(AudioClip[] set) => set != null && set.Length > 0;

    private void Play(AudioClip[] set)
    {
        if (source == null || !HasClips(set)) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(set[Random.Range(0, set.Length)], volume);
    }
}
