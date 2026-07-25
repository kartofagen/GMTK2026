using UnityEngine;

/// <summary>
/// Loops a popping sound while the popcorn is actively cooking, layered on top
/// of the microwave's own sounds. Observes the dish's own DishComponent rather
/// than the microwave, so nothing else has to change: a rising temperature
/// means the component is being heated right now, so the popping plays; when the
/// temperature stops climbing (idle, paused/cooling, or done) the popping stops.
/// </summary>
public class PopcornCookingSound : MonoBehaviour
{
    [SerializeField] private AudioSource cookingSource;
    [SerializeField] private AudioClip cookingClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField,
     Tooltip("Seconds without a temperature rise before the popping stops")]
    private float stopAfterIdle = 0.3f;

    private DishComponent _component;
    private float _lastTemp;
    private float _lastRiseTime = -999f;

    private void Awake()
    {
        _component = GetComponent<DishComponent>();

        if (cookingSource != null)
        {
            cookingSource.clip = cookingClip;
            cookingSource.loop = true;
            cookingSource.playOnAwake = false;
            cookingSource.volume = volume;
        }

        if (_component != null)
        {
            _lastTemp = _component.CurrentTemp;
        }
    }

    private void Update()
    {
        if (cookingSource == null || cookingClip == null || _component == null) return;

        var temp = _component.CurrentTemp;
        if (temp > _lastTemp + 0.0001f)
        {
            _lastRiseTime = Time.time;
        }
        _lastTemp = temp;

        var cooking = Time.time - _lastRiseTime <= stopAfterIdle;

        if (cooking && !cookingSource.isPlaying)
        {
            cookingSource.Play();
        }
        else if (!cooking && cookingSource.isPlaying)
        {
            cookingSource.Stop();
        }
    }
}
