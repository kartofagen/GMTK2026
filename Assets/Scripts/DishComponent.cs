using UnityEngine;
using Zenject;

public enum DishComponentStatus
{
    Ready,
    NotReady,
    Explodes
}

public class DishComponent : MonoBehaviour
{
    public string componentName;
    [SerializeField] private AnimationCurve heatingCurve;
    [SerializeField,
     Tooltip("Время, за которое еда готова (по нижней границе)")] private float readyTime; 
    
    [Inject] private GameConfig _gameConfig;

    private float _time;
    private float currentTempChange;
    
    public float CurrentTemp => _gameConfig.startTemp + currentTempChange;
    
    public void Heat(float deltaTime)
    {
        _time += deltaTime;
        var x = _time / (readyTime * _gameConfig.readyCoeff);
        currentTempChange = _gameConfig.power * heatingCurve.Evaluate(x);
        Debug.Log($"Температура: {CurrentTemp}, Взрыв: {_gameConfig.explosionThreshold}");
    }

    public DishComponentStatus GetStatus()
    {
        if (_gameConfig.targetTempRange.x <= CurrentTemp && CurrentTemp <= _gameConfig.targetTempRange.y)
        {
            return DishComponentStatus.Ready;
        }

        if (CurrentTemp > _gameConfig.explosionThreshold)
        {
            return DishComponentStatus.Explodes;
        }
        
        return DishComponentStatus.NotReady;
    }
}
