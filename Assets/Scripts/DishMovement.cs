using UnityEngine;

public class DishMovement : MonoBehaviour
{
    private bool IsChosen = false;
    
    private void OnMouseDown()
    {
        Debug.Log("OnMouseDown");
        
        if (IsChosen) return;
        
        transform.parent.GetComponent<DishesServingManager>().Serve(transform);
        IsChosen = true;
    }
}
