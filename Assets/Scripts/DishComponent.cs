using R3;
using UnityEngine;
using Zenject;

public enum DishComponentStatus
{
    Ready,
    NotReady,
    Explodes
}

public class DishComponent : MonoBehaviour, ITemperatureChannel
{
    public string componentName;
    [SerializeField] private AnimationCurve heatingCurve;
    [SerializeField,
     Tooltip("Время, за которое еда готова (по нижней границе)")] private float readyTime;

    [Inject] private GameConfig _gameConfig;

    private float _heatingTime;
    private float _currentTempDelta;
    private float _readyTempDelta;
    private float _readyTempCoeff;

    private bool _isCooling;
    private float _coolingTime;
    private float _coolingStartTemp;

    private readonly ReactiveProperty<float> _temperature = new();

    // ITemperatureChannel
    public string Name => componentName;
    public ReadOnlyReactiveProperty<float> Temperature => _temperature;
    
    public DishComponentStatus DishComponentStatus { get; private set; } = DishComponentStatus.NotReady;

    public float CurrentTemp => _temperature.Value;

    void Awake()
    {
        _readyTempDelta = _gameConfig.targetTempRange.x - _gameConfig.startTemp;
        _readyTempCoeff = _readyTempDelta / heatingCurve.Evaluate(_gameConfig.readyCoeff);
        _temperature.Value = _gameConfig.startTemp;
    }
    
    public void Heat(float deltaTime)
    {
        if (DishComponentStatus == DishComponentStatus.Explodes)
        {
            _temperature.Value = 1000f;
            return;
        }
        
        if (_isCooling)
        {
            _isCooling = false;
            
            var targetCurveValue = _currentTempDelta / (_readyTempCoeff * _gameConfig.power);
            var xToContinue = InverseEvaluateCurve(heatingCurve, targetCurveValue);
            _heatingTime = xToContinue * (readyTime / _gameConfig.readyCoeff);
        }
        
        _heatingTime += deltaTime;
        var x = _heatingTime / (readyTime / _gameConfig.readyCoeff);
        _currentTempDelta = heatingCurve.Evaluate(x) * _readyTempCoeff * _gameConfig.power;
        _temperature.Value = _gameConfig.startTemp + _currentTempDelta;
    }
    
    public void Cool(float deltaTime, float averageTemp)
    {
        if (DishComponentStatus == DishComponentStatus.Explodes)
        {
            return;
        }
        
        var temp = Mathf.MoveTowards(CurrentTemp, averageTemp, 
            _gameConfig.coolingToAverageSpeed * deltaTime);

        if (!_isCooling)
        {
            _isCooling = true;
            _coolingTime = 0f;
            _coolingStartTemp = temp;
        }
        _coolingTime += deltaTime;

        var curveValue = _gameConfig.coolingCurve.Evaluate(_coolingTime / _gameConfig.fullCoolingTime);
        temp = Mathf.Lerp(_coolingStartTemp, _gameConfig.roomTemp, 1 - curveValue);
        temp = Mathf.Max(temp, _gameConfig.roomTemp);

        _currentTempDelta = temp - _gameConfig.startTemp;
        _temperature.Value = _gameConfig.startTemp + _currentTempDelta;
    }
    
    public DishComponentStatus GetStatus()
    {
        if (_gameConfig.targetTempRange.x <= CurrentTemp && CurrentTemp <= _gameConfig.targetTempRange.y)
        {
            DishComponentStatus = DishComponentStatus.Ready;
        } 
        else if (CurrentTemp > _gameConfig.explosionThreshold)
        {
            DishComponentStatus = DishComponentStatus.Explodes;
        }
        else
        {
            DishComponentStatus = DishComponentStatus.NotReady;
        }
        
        return DishComponentStatus;
    }
    
    private static float InverseEvaluateCurve(AnimationCurve curve, float targetValue)
    {
        var left = 0f;
        var right = 1f;

        for (var i = 0; i < 32; ++i)
        {
            var middle = (left + right) / 2;
            if (curve.Evaluate(middle) < targetValue)
            {
                left = middle;
            }
            else
            {
                right = middle;
            }
        }

        return (left + right) / 2;
    }

}
