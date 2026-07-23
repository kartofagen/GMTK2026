using System;
using R3;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CooldownButton : MonoBehaviour
{
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private UnityEvent onClick;

    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();

        Observable.FromEvent(
                h => new UnityAction(h),
                h => _button.onClick.AddListener(h),
                h => _button.onClick.RemoveListener(h)
            )
            .ThrottleFirst(TimeSpan.FromSeconds(cooldown))
            .Subscribe(_ => onClick.Invoke())
            .AddTo(this);
    }
}
