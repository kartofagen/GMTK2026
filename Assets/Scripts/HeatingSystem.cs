using UnityEngine;

public class HeatingSystem : MonoBehaviour
{
    public Vector2 targetTempRange = new Vector2(65f, 75f);
    public float startTemp = 5f;
    public float explosionThreshold = 100f;
    
    [Header("Cooling")]
    public AnimationCurve coolingCurve;
    public float roomTemp = 20f;
    public float coolingToAverageSpeed = 1f;
    
    private MicrowaveTimer microwaveTimer;

    void Awake()
    {
        microwaveTimer = GetComponent<MicrowaveTimer>();
    }
}
