using UnityEngine;

public class DishMovement : MonoBehaviour
{
    private bool _isChosen = false;
    
    private void OnMouseDown()
    {
        if (_isChosen)
        {
            
        }
        
        transform.parent.GetComponent<DishesServingManager>().Serve(transform);
        _isChosen = true;
    }
}
