using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// Гоняет тепловую модель загруженного блюда с фиксированным шагом и связывает её
/// с микроволновкой: таймер даёт управление u, взрыв останавливает печь, открытие
/// дверцы (оно же FinishHeating) фиксирует результат.
/// </summary>
public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    [SerializeField] private MeshRenderer microwaveBodyRenderer;
    [SerializeField] private Texture2D dirtyTexture;

    [SerializeField,
     Tooltip("Предохранитель: сколько шагов симуляции максимум за кадр при просадке FPS")]
    private int maxStepsPerFrame = 8;

    [Inject] private MicrowaveContext _context;

    private MicrowaveTimer _microwaveTimer;
    private float _accumulator;

    public Dish Dish
    {
        set
        {
            dish = value;
            Bind();
        }
    }

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();
    }

    void Start()
    {
        _microwaveTimer.onFinished
            .Subscribe(_ => OnHeatingFinished())
            .AddTo(this);

        // Блюдо могло быть задано в инспекторе, а не через сеттер.
        if (dish) Bind();
    }

    void Update()
    {
        if (!dish || !dish.Level) return;

        // Шаг фиксированный: траектория температур не должна зависеть от FPS.
        float dt = 1f / dish.Level.simRate;
        float u = _microwaveTimer.State == MicrowaveState.Heating ? 1f : 0f;

        _accumulator += Time.deltaTime;

        int steps = 0;
        while (_accumulator >= dt && steps < maxStepsPerFrame)
        {
            _accumulator -= dt;
            steps++;
            dish.Tick(u, dt);
        }

        // При долгой просадке не копим неоплатный долг по времени.
        if (_accumulator > dt * maxStepsPerFrame) _accumulator = 0f;
    }

    private void Bind()
    {
        if (!dish)
        {
            _context.Channels.Value = null;
            _context.Level.Value = null;
            return;
        }

        _context.Level.Value = dish.Level;
        _context.Channels.Value = dish.Channels;

        // Блюдо могло быть и задано в инспекторе, и подано сеттером — не подписываемся дважды.
        dish.OnExplosion -= OnExplosion;
        dish.OnExplosion += OnExplosion;

        _accumulator = 0f;
    }

    // Печь после взрыва не выключается: блюдо уже проиграно, но игрок волен догреть
    // остальные продукты и разнести их тоже.
    private void OnExplosion() => PlayExplosionEffects();

    private void PlayExplosionEffects()
    {
        microwaveBodyRenderer.material.SetTexture("_BaseMap", dirtyTexture);
    }

    /// <summary>Печь остановилась — таймер вышел, нажали стоп или открыли дверцу.</summary>
    private void OnHeatingFinished()
    {
        if (!dish) return;

        var result = dish.EvaluateResult();
        Debug.Log($"Блюдо «{dish.DishName}»: {result}");
    }

    private void OnDestroy()
    {
        if (dish) dish.OnExplosion -= OnExplosion;
    }
}
