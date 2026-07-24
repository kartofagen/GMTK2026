using UnityEngine;

public class MovingInsideSystem : MonoBehaviour
{
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform targetPoint;
    
    public Transform EntryPoint => entryPoint;
    public Transform TargetPoint => targetPoint;
}
