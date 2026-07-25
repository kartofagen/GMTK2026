using UnityEngine;

/// <summary>
/// Plays an explosion when the dish overheats. Watches the public DishStatus
/// on the Dish this sits on, so the heating logic stays untouched. Fires once,
/// on the transition into Exploded, which the dish latches - it never leaves
/// that state, so the sound cannot retrigger.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DishExplosionSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] explosionClips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private Dish _dish;
    private AudioSource _source;
    private bool _exploded;

    private void Awake()
    {
        _dish = GetComponent<Dish>();
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    private void Update()
    {
        if (_exploded || _dish == null) return;
        if (_dish.DishStatus != DishStatus.Exploded) return;

        _exploded = true;

        var clip = PickClip();
        if (clip != null)
        {
            _source.PlayOneShot(clip, volume);
        }
    }

    private AudioClip PickClip()
    {
        if (explosionClips == null || explosionClips.Length == 0) return null;

        return explosionClips[Random.Range(0, explosionClips.Length)];
    }
}
