using UnityEngine;

/// <summary>
/// Plays a click when the button this sits on is pressed.
/// Unity delivers OnMouseDown to every component on the collider's GameObject,
/// so this rides the same press as MicrowaveButton without touching it.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MicrowaveButtonSound : MonoBehaviour
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.97f, 1.03f);
    [SerializeField] private bool randomizePerPress = false;

    private AudioSource _source;
    private AudioClip _clip;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;

        _clip = randomizePerPress ? null : PickStableClip();
    }

    private void OnMouseDown()
    {
        var clip = randomizePerPress ? PickRandomClip() : _clip;
        if (clip == null) return;

        _source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        _source.PlayOneShot(clip, volume);
    }

    /// <summary>
    /// Picks a clip from the button's name and position in the hierarchy, so each
    /// button keeps the same voice across play sessions while the spread across
    /// buttons stays arbitrary. Sibling index keeps same-named buttons apart.
    /// Uses an explicit FNV-1a hash rather than string.GetHashCode, which is not
    /// guaranteed to be stable across runtimes or platforms.
    /// </summary>
    private AudioClip PickStableClip()
    {
        if (clips == null || clips.Length == 0) return null;

        var hash = 2166136261u;
        foreach (var c in name)
        {
            hash = (hash ^ c) * 16777619u;
        }
        hash = (hash ^ (uint)transform.GetSiblingIndex()) * 16777619u;

        return clips[(int)(hash % (uint)clips.Length)];
    }

    private AudioClip PickRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;

        return clips[Random.Range(0, clips.Length)];
    }
}
