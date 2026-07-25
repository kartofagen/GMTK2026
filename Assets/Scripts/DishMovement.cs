using UnityEngine;

public enum DishMovementState
{
    Idle,
    Outside,
    Inside
}

public class DishMovement : MonoBehaviour
{
    public DishMovementState MovementState { get; set; } = DishMovementState.Idle;
    
    private void OnMouseDown()
    {
        switch (MovementState)
        {
            case DishMovementState.Idle:
                transform.parent.GetComponent<DishesServingManager>()?.Serve(transform);
                MovementState = DishMovementState.Outside;
                break;
            case DishMovementState.Inside:
                transform.parent.GetComponent<MovingInsideSystem>()?.MoveOutside();
                MovementState = DishMovementState.Outside;
                break;
            case DishMovementState.Outside:
                break;
        }
    }
}
