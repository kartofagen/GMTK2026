using UnityEngine;

/// <summary>
/// One cooking sound for a group of like components (e.g. the sausages in a
/// three-sausage dish), choosing the clip by how many are still cooking instead
/// of stacking a separate per-item loop. As components explode the active count
/// drops and the sound switches to the clip for that count; when none are left
/// it goes silent.
///
/// clipsByCount is indexed by (active count - 1): element 0 is the sound for one
/// item, element 1 for two, and so on.
/// </summary>
public class GroupCookingSound : MonoBehaviour
{
    [SerializeField] private AudioSource cookingSource;
    [SerializeField] private AudioClip[] clipsByCount;
    [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
    [SerializeField, Tooltip("Only components with this name are counted. Empty counts all.")]
    private string filterName = "";
    [SerializeField, Tooltip("Seconds without a temperature rise before the sound stops")]
    private float stopAfterIdle = 0.3f;

    private DishComponent[] _components;
    private float[] _lastTemp;
    private float _lastRiseTime = -999f;
    private int _playingCount;

    private void Awake()
    {
        var all = GetComponentsInChildren<DishComponent>(true);
        if (string.IsNullOrEmpty(filterName))
        {
            _components = all;
        }
        else
        {
            int n = 0;
            foreach (var c in all)
            {
                if (c.Name == filterName) n++;
            }
            _components = new DishComponent[n];
            int j = 0;
            foreach (var c in all)
            {
                if (c.Name == filterName) _components[j++] = c;
            }
        }

        _lastTemp = new float[_components.Length];
        for (int i = 0; i < _components.Length; i++)
        {
            _lastTemp[i] = _components[i].CurrentTemp;
        }

        if (cookingSource != null)
        {
            cookingSource.loop = true;
            cookingSource.playOnAwake = false;
            cookingSource.volume = volume;
        }
    }

    private void Update()
    {
        if (cookingSource == null || _components.Length == 0) return;

        var active = 0;
        var rising = false;
        for (int i = 0; i < _components.Length; i++)
        {
            var c = _components[i];
            if (!c.Stopped.CurrentValue)
            {
                active++;
                if (c.CurrentTemp > _lastTemp[i] + 0.0001f)
                {
                    rising = true;
                }
            }
            _lastTemp[i] = c.CurrentTemp;
        }

        if (rising)
        {
            _lastRiseTime = Time.time;
        }

        var cooking = active > 0 && Time.time - _lastRiseTime <= stopAfterIdle;
        var desired = cooking ? active : 0;

        if (desired == _playingCount)
        {
            if (desired > 0 && !cookingSource.isPlaying)
            {
                cookingSource.Play();
            }
            return;
        }

        _playingCount = desired;

        var clip = desired > 0 && desired - 1 < clipsByCount.Length ? clipsByCount[desired - 1] : null;
        cookingSource.clip = clip;
        if (clip != null)
        {
            cookingSource.Play();
        }
        else
        {
            cookingSource.Stop();
        }
    }
}
