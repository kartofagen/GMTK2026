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
    private float _currentTempDelta;
    private float _readyTempDelta;
    private float _readyTempCoeff;

    private bool _isCooling;
    private float _coolingTime;
    private float _coolingStartTemp;
    
    public float CurrentTemp => _gameConfig.startTemp + _currentTempDelta;

    void Awake()
    {
        _readyTempDelta = _gameConfig.targetTempRange.x - _gameConfig.startTemp;
        _readyTempCoeff = _readyTempDelta / heatingCurve.Evaluate(_gameConfig.readyCoeff);
    }
    
    public void Heat(float deltaTime)
    {
        _time += deltaTime;
        var x = _time / (readyTime / _gameConfig.readyCoeff);
        _currentTempDelta = heatingCurve.Evaluate(x) * _readyTempCoeff * _gameConfig.power;
        
        Debug.Log($"Температура: {CurrentTemp}, Взрыв: {_gameConfig.explosionThreshold}");
    }
    
    public void Cool(float deltaTime, float averageTemp)
    {
        var temp = Mathf.MoveTowards(CurrentTemp, averageTemp, 
            _gameConfig.coolingToAverageSpeed * deltaTime);

        if (!_isCooling)
        {
            _isCooling = true;
            _coolingTime = 0f;
            _coolingStartTemp = temp;
        }
        _coolingTime += deltaTime;

        var curveValue = _gameConfig.coolingCurve.Evaluate(_coolingTime);
        temp = Mathf.Lerp(_coolingStartTemp, _gameConfig.roomTemp, 1 - curveValue);
        temp = Mathf.Max(temp, _gameConfig.roomTemp);

        _currentTempDelta = temp - _gameConfig.startTemp;
        
        Debug.Log($"Охлаждение: {CurrentTemp}");
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
