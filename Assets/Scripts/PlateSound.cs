using UnityEngine;

public class PlateSound : MonoBehaviour
{
    [SerializeField] private AudioSource plateSource;
    [SerializeField] private AudioClip plateClip;
    [SerializeField, Range(0f, 1f)] private float plateVolume = 1f;
    
    private void Awake()
    {
        if (plateSource == null) return;

        if (plateClip != null) plateSource.clip = plateClip;
        plateSource.loop = false;
        plateSource.playOnAwake = false;
        plateSource.volume = plateVolume;
    }

    public void PlatePlaced()
    {
        if (plateSource == null || plateClip == null) return;

        plateSource.PlayOneShot(plateClip, plateVolume);
    }
}
