using System;
using System.Collections.Generic;
using UnityEngine;

public enum DishStatus
{
    InProgress,
    Success,
    Underheated,
    Exploded
}

public class Dish : MonoBehaviour
{
    public string dishName;

    [SerializeField, Tooltip("Параметры продуктов и печи. Порядок компонентов должен совпадать с components")]
    private LevelConfig level;

    [SerializeField] private DishComponent[] components;

    private ThermalSolver _solver;

    // По флагу на продукт: взрыв — разовое событие, но рвануть может каждый.
    private bool[] _exploded;

    public DishStatus DishStatus { get; private set; } = DishStatus.InProgress;

    public string DishName => dishName;

    public LevelConfig Level => level;

    /// <summary>Каналы температуры по компонентам — по одному на серию графика.</summary>
    public IReadOnlyList<ITemperatureChannel> Channels => components;

    public event Action OnExplosion;

    void Awake()
    {
        if (!level)
        {
            Debug.LogError($"{name}: не задан LevelConfig — блюдо не будет греться", this);
            return;
        }

        if (level.ComponentCount != components.Length)
        {
            Debug.LogError(
                $"{name}: в LevelConfig {level.ComponentCount} компонент(ов), а на сцене {components.Length}. " +
                "Соответствие идёт по индексу, поэтому количества должны совпадать", this);
            return;
        }

        _solver = new ThermalSolver(level);
        _exploded = new bool[components.Length];
        PushTemperatures();
    }

    /// <summary>
    /// Один шаг симуляции. u = 1, когда печь включена; остывание — это тот же шаг при u = 0.
    /// Взрыв одного продукта не останавливает симуляцию: остальные продолжают греться
    /// и тоже могут рвануть, каждый по своему потолку.
    /// </summary>
    public void Tick(float u, float dt)
    {
        if (_solver == null) return;

        _solver.Step(u, dt);
        PushTemperatures();

        // Потолок tMax нельзя пробивать НИ В ОДИН момент времени — проверяем на каждом шаге.
        for (int i = 0; i < components.Length; i++)
        {
            if (_exploded[i] || !_solver.IsOverCeiling(i)) continue;

            _exploded[i] = true;
            DishStatus = DishStatus.Exploded;
            components[i].Explode();
            OnExplosion?.Invoke();
        }
    }

    /// <summary>
    /// Итог на момент остановки («открыли дверцу»): успех, если все компоненты
    /// одновременно попали в свои целевые окна.
    /// </summary>
    public DishStatus EvaluateResult()
    {
        if (DishStatus == DishStatus.Exploded) return DishStatus;

        DishStatus = _solver != null && _solver.AllInTargetWindow()
            ? DishStatus.Success
            : DishStatus.Underheated;

        return DishStatus;
    }

    private void PushTemperatures()
    {
        for (int i = 0; i < components.Length; i++)
        {
            components[i].SetTemperature(_solver[i]);
        }
    }
}
