using UnityEngine;

/// <summary>
/// Plays a sound when the player takes this dish from the table.
/// Rides Unity's OnMouseDown broadcast alongside DishMovement rather than
/// hooking into it, so the dish-moving logic stays untouched. Fires once, on
/// the first take, mirroring DishMovement's own single-use guard.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DishTakeSound : MonoBehaviour
{
    [SerializeField] private AudioClip takeClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private AudioSource _source;
    private bool _taken;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    private void OnMouseDown()
    {
        if (_taken) return;
        _taken = true;

        if (takeClip != null)
        {
            _source.PlayOneShot(takeClip, volume);
        }
    }
}
