using UnityEngine;

/// <summary>
/// Plays the door's open and close sounds by watching DoorRotation's public
/// IsOpened flag, leaving the door logic itself untouched. Polls rather than
/// listening for OnMouseDown, because both components would receive that
/// message in an undefined order and the flag flips inside the handler.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MicrowaveDoorSound : MonoBehaviour
{
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    private DoorRotation _door;
    private AudioSource _source;
    private bool _wasOpened;

    private void Awake()
    {
        _door = GetComponent<DoorRotation>();
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;

        _wasOpened = _door.IsOpened;
    }

    private void Update()
    {
        var opened = _door.IsOpened;
        if (opened == _wasOpened) return;

        var clip = opened ? openClip : closeClip;
        if (clip != null)
        {
            _source.PlayOneShot(clip, volume);
        }

        _wasOpened = opened;
    }
}
