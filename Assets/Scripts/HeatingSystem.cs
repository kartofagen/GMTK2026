using R3;
using UnityEngine;
using Zenject;

/// <summary>
/// Гоняет тепловую модель загруженного блюда с фиксированным шагом и связывает её
/// с микроволновкой: таймер даёт управление u, а остановка печи (таймер вышел, нажали
/// стоп или открыли дверцу) фиксирует результат блюда.
/// </summary>
public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    [SerializeField] private MeshRenderer microwaveBodyRenderer;
    [SerializeField] private Texture2D dirtyTexture;

    [SerializeField,
     Tooltip("Предохранитель: сколько шагов симуляции максимум за кадр при просадке FPS")]
    private int maxStepsPerFrame = 8;

    [SerializeField, Tooltip("Скорость вращения тарелки во время нагрева, град/с")]
    private float dishRotationSpeed = 5f;

    [Inject] private MicrowaveContext _context;

    private MicrowaveTimer _microwaveTimer;
    private float _accumulator;

    public readonly Subject<DishStatus> onHeatingFinished = new();

    public Dish Dish
    {
        get => dish;
        set
        {
            if (dish) dish.OnExplosion -= OnExplosion;

            dish = value;
            Bind();
        }
    }

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();

        _microwaveTimer.onFinished
            .Subscribe(OnFinished)
            .AddTo(this);
    }

    void Start()
    {
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

        // Тарелка крутится, пока идёт нагрев. Это чистая визуалка, поэтому шаг
        // здесь кадровый, а не фиксированный шаг симуляции: на неё не должно
        // влиять, сколько раз за кадр провернулась физика.
        if (u != 0f)
        {
            dish.transform.Rotate(Vector3.up, dishRotationSpeed * Time.deltaTime);
        }
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
    private void OnFinished(Unit unit)
    {
        if (!dish) return;

        onHeatingFinished.OnNext(dish.GetFinalStatus());
    }

    private void OnDestroy()
    {
        if (dish) dish.OnExplosion -= OnExplosion;
    }
}
