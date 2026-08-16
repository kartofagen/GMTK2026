using DG.Tweening;
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
    private AudioClip[] plateBreakClips;
    [SerializeField, Tooltip("Overheated: the ouch when the hot plate is grabbed")]
    private AudioClip[] overheatDropClips;
    [SerializeField]
    private AudioClip[] overheatEatClips;
    [SerializeField, Tooltip("Successful dishes - tasty!")]
    private AudioClip[] successClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

    private HeatingSystem _heating;
    private MovingInsideSystem _moving;
    private DishStatus _lastStatus = DishStatus.InProgress;
    
    private Tween _delayTween;

    private bool _hasPlate = false;

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

        _moving.onDishOutside
            .Subscribe(_ => OnDishOutside())
            .AddTo(this);

        _moving.onDishTouched
            .Subscribe(_ => OnDishTouched())
            .AddTo(this);
        
        _moving.onDishInside
            .Subscribe(SetHasPlate)
            .AddTo(this);
    }

    private void SetHasPlate(Transform dish)
    {
        _hasPlate = dish.GetComponentInChildren<PlateSound>() != null;
    }
    
    private void OnDishOutside()
    {
        if (!_hasPlate) return;
        
        if (_lastStatus == DishStatus.Success) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(plateBreakClips[Random.Range(0, plateBreakClips.Length)], volume);

        _lastStatus = DishStatus.InProgress;
    }
    
    private void OnDishTouched()
    {
        Play(_lastStatus switch
        {
            DishStatus.Success => successClips,
            DishStatus.Overheating => overheatEatClips,
            DishStatus.Exploded => overheatDropClips,
            _ => null
        });
    }

    private void PlayBreakDelayed() => Play(plateBreakClips);

    private static bool HasClips(AudioClip[] set) => set != null && set.Length > 0;

    private void Play(AudioClip[] set)
    {
        if (source == null || !HasClips(set)) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(set[Random.Range(0, set.Length)], volume);
    }
}
