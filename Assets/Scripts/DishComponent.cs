using R3;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Как разлетается продукт при взрыве.
/// </summary>
public enum ExplodeMethod
{
    /// <summary>Куски липнут к стенкам камеры по лучу.</summary>
    StickToWalls,

    /// <summary>Куски разлетаются физикой: Rigidbody + импульс.</summary>
    Simulation
}

/// <summary>
/// Продукт на тарелке: подача его температуры на график и эффекты взрыва.
/// Саму температуру считает ThermalSolver по параметрам из LevelConfig — здесь
/// только представление, поэтому числовые параметры продукта живут в ассете уровня.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DishComponent : MonoBehaviour, ITemperatureChannel
{
    public string componentName;

    [SerializeField] private ExplodeMethod explodeMethod;
    [SerializeField] private GameObject[] explosionParts;
    [SerializeField] private float explodePointOffset = 0.1f;
    [SerializeField] private int explosionParticles = 10;
    [SerializeField] private AudioClip[] explosionSounds;

    [SerializeField, Tooltip("Импульс, с которым разлетаются куски в режиме Simulation")]
    private float explosionPower = 1f;

    private bool _exploded;
    private AudioSource _source;

    private readonly ReactiveProperty<float> _temperature = new();

    // ITemperatureChannel
    public string Name => componentName;
    public ReadOnlyReactiveProperty<float> Temperature => _temperature;

    public float CurrentTemp => _temperature.Value;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
    }

    public void SetTemperature(float value) => _temperature.Value = value;

    /// <summary>Взрыв: разлёт кусков и звук. Повторные вызовы игнорируются.</summary>
    public void Explode()
    {
        if (_exploded) return;
        _exploded = true;
        
        SetTemperature(1000f);

        if (explosionSounds.Length > 0)
        {
            _source.PlayOneShot(explosionSounds[Random.Range(0, explosionSounds.Length)], 1f);
        }

        var explosionPoint = transform.position + new Vector3(0, explodePointOffset, 0);

        for (int i = 0; i < explosionParticles; ++i)
        {
            var particleDirection = Vector3.forward;
            particleDirection = Quaternion.AngleAxis(Random.Range(30f, -90f), Vector3.right) * particleDirection;
            particleDirection = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up) * particleDirection;

            if (explodeMethod == ExplodeMethod.StickToWalls)
            {
                var ray = new Ray(explosionPoint, particleDirection);

                if (Physics.Raycast(ray, out var hit, 1))
                {
                    // Чуть утапливаем в стенку, иначе кусок висит в воздухе,
                    // и разворачиваем по нормали — тогда он выглядит прилипшим.
                    hit.point -= particleDirection * 0.05f;
                    var particle = Instantiate(explosionParts[Random.Range(0, explosionParts.Length)],
                        hit.point, Random.rotation);
                    particle.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
            }
            else
            {
                var particle = Instantiate(explosionParts[Random.Range(0, explosionParts.Length)],
                    explosionPoint, Random.rotation);
                var rb = particle.AddComponent<Rigidbody>();
                rb.AddForce(particleDirection * explosionPower, ForceMode.Impulse);
            }
        }

        SetVisible(false);
    }

    /// <summary>Блюдо пошло на второй заход: собираем продукт обратно.</summary>
    public void Reset()
    {
        _exploded = false;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        var meshRenderer = GetComponentInChildren<MeshRenderer>(true);
        if (meshRenderer) meshRenderer.enabled = visible;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(0, explodePointOffset, 0));
    }
}
