using System;
using R3;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

public enum DishComponentStatus
{
    Ready,
    NotReady,
    Explodes
}

[RequireComponent(typeof(AudioSource))]
public class DishComponent : MonoBehaviour, ITemperatureChannel
{
    public string componentName;
    [SerializeField] private AnimationCurve heatingCurve;

    [SerializeField,
     Tooltip("Время, за которое еда готова (по нижней границе)")]
    private float readyTime;

    [SerializeField] private GameObject[] explosionParts;
    [SerializeField] private float explodePointOffset = 0.1f;
    [SerializeField] private int explosionParticles = 10;
    [SerializeField] private AudioClip[] explosionSounds;

    [Inject] private GameConfig _gameConfig;

    private float _heatingTime;
    private float _currentTempDelta;
    private float _readyTempDelta;
    private float _readyTempCoeff;

    private bool _isCooling;
    private float _coolingTime;
    private float _coolingStartTemp;

    private bool _exploded;
    private AudioSource _source;

    private readonly ReactiveProperty<float> _temperature = new();

    // ITemperatureChannel
    public string Name => componentName;
    public ReadOnlyReactiveProperty<float> Temperature => _temperature;

    private DishComponentStatus DishComponentStatus { get; set; } = DishComponentStatus.NotReady;

    public float CurrentTemp => _temperature.Value;

    void Awake()
    {
        _readyTempDelta = (_gameConfig.targetTempRange.x + _gameConfig.targetTempRange.y) * 0.5f - _gameConfig.startTemp;
        _readyTempCoeff = _readyTempDelta / heatingCurve.Evaluate(_gameConfig.readyCoeff);
        _temperature.Value = _gameConfig.startTemp;
        
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void Heat(float deltaTime)
    {
        if (DishComponentStatus == DishComponentStatus.Explodes)
        {
            _temperature.Value = 1000f;
            if (!_exploded)
            {
                _exploded = true;
                Explode();
            }

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

        Debug.Log($"Температура: {CurrentTemp}, Взрыв: {_gameConfig.explosionThreshold}");
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

    private void Explode()
    {
        Debug.Log("Spawning explosion parts from " + componentName);
        _source.PlayOneShot(explosionSounds[Random.Range(0, explosionSounds.Length)], 1f);
        var explosionPoint = transform.position + new Vector3(0, explodePointOffset, 0);
        for (int i = 0; i < explosionParticles; ++i)
        {
            var particleDirection = Vector3.forward;
            particleDirection = Quaternion.AngleAxis(Random.Range(30f, -90f), Vector3.right) * particleDirection;
            particleDirection = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * particleDirection;
            
            var ray = new Ray(explosionPoint, particleDirection);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1))
            {
                hit.point -= particleDirection * 0.05f;
                Instantiate(explosionParts[Random.Range(0, explosionParts.Length)], hit.point, Random.rotation);
            }

            Debug.Log($"Spawned part {i}");
        }

        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;
    }

    private static float InverseEvaluateCurve(AnimationCurve curve, float targetValue)
    {
        var left = 0f;
        var right = 1f;

        for (var i = 0; i < 32; ++i)
        {
            var middle = (left + right) * 0.5f;
            if (curve.Evaluate(middle) < targetValue)
            {
                left = middle;
            }
            else
            {
                right = middle;
            }
        }

        return (left + right) * 0.5f;
    }

    public void Reset()
    {
        DishComponentStatus = DishComponentStatus.NotReady;
        
        _temperature.Value = _gameConfig.startTemp;
        _heatingTime = 0f;
        _currentTempDelta = 0f;
        
        _isCooling = false;
        _exploded = false;
        
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = true;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, explodePointOffset, 0));
    }
}
