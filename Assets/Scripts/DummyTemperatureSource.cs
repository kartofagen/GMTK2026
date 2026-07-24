using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// Тестовый источник температуры: публикует несколько фейковых каналов в MicrowaveContext,
/// чтобы график можно было проверить без реального блюда.
/// Просто повесь на любой объект в тестовой сцене (в боевой — убрать).
/// </summary>
public class DummyTemperatureSource : MonoBehaviour
{
    [Tooltip("Сколько тестовых каналов (серий) генерировать")]
    [SerializeField] private int channelCount = 2;

    [Tooltip("Базовая температура, вокруг которой колеблется значение")]
    [SerializeField] private float baseTemp = 50f;

    [Tooltip("Амплитуда синусоиды")]
    [SerializeField] private float amplitude = 30f;

    [Tooltip("Скорость колебания (циклов в секунду)")]
    [SerializeField] private float frequency = 0.2f;

    [Tooltip("Случайный шум поверх синусоиды")]
    [SerializeField] private float noise = 3f;

    [Inject] private MicrowaveContext _context;

    private DummyChannel[] _channels;

    void Start()
    {
        _channels = new DummyChannel[channelCount];
        for (int i = 0; i < channelCount; i++)
        {
            // Сдвигаем фазу, чтобы линии не сливались.
            _channels[i] = new DummyChannel($"Тест {i + 1}", i / (float)channelCount);
        }

        _context.Channels.Value = _channels;
    }

    void Update()
    {
        if (_channels == null) return;

        float t = Time.time;
        foreach (var channel in _channels)
        {
            float value =
                baseTemp
                + amplitude * Mathf.Sin((t * frequency + channel.Phase) * 2f * Mathf.PI)
                + Random.Range(-noise, noise);

            channel.Set(value);
        }
    }

    private class DummyChannel : ITemperatureChannel
    {
        private readonly ReactiveProperty<float> _temperature = new();

        public string Name { get; }
        public float Phase { get; }
        public ReadOnlyReactiveProperty<float> Temperature => _temperature;

        public DummyChannel(string name, float phase)
        {
            Name = name;
            Phase = phase;
        }

        public void Set(float value) => _temperature.Value = value;
    }
}
