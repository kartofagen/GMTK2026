using UnityEngine;

[CreateAssetMenu]
public class GameConfig : ScriptableObject
{
    [Header("Heating")]
    public float power = 100f;
    public Vector2 targetTempRange = new Vector2(65f, 75f);
    public float startTemp = 5f;
    public float explosionThreshold = 100f;
    
    [Tooltip("Время нижней границы готовности от 0 до 1 по оси x")]
    public float readyCoeff = 0.75f;
    
    [Header("Cooling")]
    public AnimationCurve coolingCurve;
    public float coolingToAverageSpeed = 1f;
    public float roomTemp = 20f;
    
    [Tooltip("Время полного остывания")]
    public float fullCoolingTime = 10f;
}
