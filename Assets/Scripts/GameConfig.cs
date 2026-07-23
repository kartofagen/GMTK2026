using UnityEngine;

[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    [Header("Heating")]
    public Vector2 targetTempRange = new Vector2(65f, 75f);
    public float startTemp = 5f;
    public float explosionThreshold = 100f;
    
    [Header("Cooling")]
    public AnimationCurve coolingCurve;
    public float coolingToAverageSpeed = 1f;
    public float roomTemp = 20f;
}


