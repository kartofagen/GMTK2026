using UnityEngine;
using Zenject;

/// <summary>
/// Тестовый источник температуры: пишет фейковые значения в TemperatureModel,
/// чтобы график можно было проверить без реального нагрева.
/// Просто повесь на любой объект в сцене.
/// </summary>
public class DummyTemperatureSource : MonoBehaviour
{
    [Tooltip("Базовая температура, вокруг которой колеблется значение")]
    [SerializeField] private float baseTemp = 50f;

    [Tooltip("Амплитуда синусоиды")]
    [SerializeField] private float amplitude = 30f;

    [Tooltip("Скорость колебания (циклов в секунду)")]
    [SerializeField] private float frequency = 0.2f;

    [Tooltip("Случайный шум поверх синусоиды")]
    [SerializeField] private float noise = 3f;

    [Tooltip("Медленный дрейф вверх (градусов в секунду)")]
    [SerializeField] private float drift = 0f;

    [Inject] private TemperatureModel _model;

    void Update()
    {
        float t = Time.time;
        float value =
            baseTemp
            + drift * t
            + amplitude * Mathf.Sin(t * frequency * 2f * Mathf.PI)
            + Random.Range(-noise, noise);

        _model.Temperature.Value = value;
    }
}
