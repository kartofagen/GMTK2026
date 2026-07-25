using UnityEngine;

/// <summary>
/// Keeps a single looping music source alive across scene loads, so switching
/// scenes never restarts or interrupts the track.
///
/// The first instance to wake claims the singleton and survives scene changes.
/// Every scene carries its own BackgroundMusic object, so when a new scene
/// loads its copy wakes, sees the original already playing, and destroys
/// itself - the original plays straight through, seamlessly.
///
/// Playback is started from code (playOnAwake is off on the source) so a
/// duplicate never sounds even for a frame before it removes itself.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicPlayer : MonoBehaviour
{
    private static BackgroundMusicPlayer _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        var source = GetComponent<AudioSource>();
        source.loop = true;
        if (!source.isPlaying)
        {
            source.Play();
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
