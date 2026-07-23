using UnityEngine;

[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    [Header("Heating")]
    public float power = 100f;
    public Vector2 targetTempRange = new Vector2(65f, 75f);
    public float startTemp = 5f;
    public float explosionThreshold = 100f;
    
    public float readyCoeff = 0.75f; // Время готовности от 0 до 1 по Ox
    
    [Header("Cooling")]
    public AnimationCurve coolingCurve;
    public float coolingToAverageSpeed = 1f;
    public float roomTemp = 20f;
}


