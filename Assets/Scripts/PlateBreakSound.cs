using R3;
using UnityEngine;

/// <summary>
/// Plays a reaction sound when a failed dish is taken out. Underheated and
/// exploded dishes are thrown away, so they get a plate-break clip; overheated
/// dishes go to the mouth and burn, so they get an "ouch" clip instead. A clean
/// success makes no sound. The final result is captured when heating finishes
/// and the sound fires when the dish actually leaves the cavity. Pitch is
/// jittered so repeats sound different. Observes the microwave's public events,
/// so nothing else has to change.
/// </summary>
public class PlateBreakSound : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField, Tooltip("Thrown-away dishes (underheated, exploded)")]
    private AudioClip[] clips;
    [SerializeField, Tooltip("Overheated dishes - the eater burns themselves")]
    private AudioClip[] overheatClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.92f, 1.08f);

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

        AudioClip[] set = null;
        if (status == DishStatus.Overheating)
        {
            set = overheatClips;
        }
        else if (status == DishStatus.Underheating || status == DishStatus.Exploded)
        {
            set = clips;
        }

        if (source == null || set == null || set.Length == 0) return;

        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(set[Random.Range(0, set.Length)], volume);
    }
}
