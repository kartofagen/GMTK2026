using R3;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Zenject;

/// <summary>
/// Кнопка с паузой между нажатиями. Длительность паузы задаёт загруженный уровень
/// (LevelConfig.cooldown) — это геймплейное ограничение, а не защита от дребезга,
/// поэтому оно должно меняться от уровня к уровню.
/// </summary>
public class CooldownButton : MonoBehaviour
{
    [SerializeField, Tooltip("Кулдаун, пока уровень не загружен или контекста нет")]
    private float cooldown = 1f;

    [SerializeField] private UnityEvent onClick;

    [InjectOptional] private MicrowaveContext _context;

    private Button _button;
    private float _lastClickTime = float.NegativeInfinity;

    /// <summary>Текущая длительность паузы: из уровня, если он загружен.</summary>
    public float Cooldown
    {
        get
        {
            var level = _context?.Level.CurrentValue;
            return level ? level.cooldown : cooldown;
        }
    }

    /// <summary>Сколько секунд осталось до разблокировки (0 — можно жать).</summary>
    public float CooldownLeft => Mathf.Max(0f, Cooldown - (Time.time - _lastClickTime));

    void Awake()
    {
        _button = GetComponent<Button>();

        // Не ThrottleFirst: его окно фиксируется на момент подписки, а кулдаун приезжает
        // вместе с уровнем уже после загрузки блюда.
        Observable.FromEvent(
                h => new UnityAction(h),
                h => _button.onClick.AddListener(h),
                h => _button.onClick.RemoveListener(h)
            )
            .Where(_ => CooldownLeft <= 0f)
            .Subscribe(_ =>
            {
                _lastClickTime = Time.time;
                onClick.Invoke();
            })
            .AddTo(this);
    }
}
