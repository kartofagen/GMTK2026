using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Один продукт на тарелке — точечный объект с одной температурой T_i(t).
/// </summary>
[Serializable]
public class LevelComponentConfig
{
    [Tooltip("Имя компонента — оно же подпись серии на графике")]
    public string name;

    [Tooltip("Цвет линии на графике")]
    public Color color = Color.white;

    [Tooltip("C — теплоёмкость: инерция нагрева. Больше C => греется медленнее")]
    public float C = 1f;

    [Tooltip("w — доля мощности печи, достающаяся компоненту (нормируется на сумму весов)")]
    public float w = 1f;

    [Tooltip("μ — коэффициент теплоотдачи в среду, 1/с")]
    public float mu = 0.01f;

    [Tooltip("Потолок: превышение в ЛЮБОЙ момент времени — взрыв")]
    public float tMax = 100f;

    [Tooltip("Низ целевого окна на момент открытия дверцы")]
    public float tOptLow = 60f;

    [Tooltip("Верх целевого окна на момент открытия дверцы")]
    public float tOptHigh = 75f;

    [Tooltip("Начальная температура")]
    public float t0 = 20f;

    [Tooltip("k(T) — форма отдачи печи в зависимости от ТЕКУЩЕЙ температуры, значение зажимается в [0,1]. " +
             "Задаёт только характер нагрева: мощность, веса, теплоёмкость, остывание и теплообмен считает движок")]
    public AnimationCurve heatCurve = AnimationCurve.Constant(0f, 200f, 1f);
}

/// <summary>
/// Конфигурация уровня для тепловой модели.
///
///   dT_i/dt = u·k_i(T_i)·P·(w_i/Σw)/C_i − μ_i·(T_i − tEnv) + Σ_j h_ij·(T_j − T_i)
///
/// u ∈ {0,1} — включена ли печь. Уровень не ограничен по времени: игрок сам решает,
/// когда открыть дверцу, и в этот момент проверяются целевые окна.
/// </summary>
[CreateAssetMenu(fileName = "Level", menuName = "Microwave/Level Config")]
public class LevelConfig : ScriptableObject
{
    [SerializeField] private List<LevelComponentConfig> components = new();

    [SerializeField,
     Tooltip("Матрица теплообмена h_ij, 1/с, построчно (N×N). Симметрична, диагональ игнорируется")]
    private float[] h = Array.Empty<float>();

    [Header("Печь")]
    [Tooltip("P — общая мощность печи, делится между компонентами по весам w")]
    public float P = 11f;

    [Tooltip("Температура окружающей среды")]
    public float tEnv = 20f;

    [Tooltip("Пауза после нажатия кнопки нагрева, с")]
    public float cooldown = 1f;

    [Header("Симуляция")]
    [Tooltip("Частота шагов симуляции, Гц. Шаг фиксированный и не зависит от FPS")]
    public float simRate = 60f;

    public IReadOnlyList<LevelComponentConfig> Components => components;

    public int ComponentCount => components.Count;

    /// <summary>
    /// h_ij с принудительной симметрией: поток из i в j — тот же поток, что из j в i,
    /// а в инспекторе легко тронуть только одну из двух ячеек. Берём среднее, чтобы
    /// правка любой из них влияла предсказуемо.
    /// </summary>
    public float H(int i, int j)
    {
        if (i == j) return 0f;

        int n = components.Count;
        if (h == null || h.Length < n * n) return 0f;

        return (h[i * n + j] + h[j * n + i]) * 0.5f;
    }

    private void OnValidate()
    {
        int n = components.Count;

        // Матрица всегда должна быть N×N, иначе H() молча вернёт нули.
        if (h == null || h.Length != n * n)
        {
            var resized = new float[n * n];
            if (h != null)
            {
                // Старые значения переносим по позиции (i,j), а не по плоскому индексу.
                int oldN = Mathf.RoundToInt(Mathf.Sqrt(h.Length));
                if (oldN * oldN == h.Length)
                {
                    int copy = Mathf.Min(oldN, n);
                    for (int i = 0; i < copy; i++)
                    for (int j = 0; j < copy; j++)
                        resized[i * n + j] = h[i * oldN + j];
                }
            }

            h = resized;
        }

        simRate = Mathf.Max(1f, simRate);

        // Явный Эйлер устойчив, пока шаг много меньше самой быстрой постоянной времени.
        float dt = 1f / simRate;
        for (int i = 0; i < n; i++)
        {
            var comp = components[i];
            if (comp == null) continue;

            if (comp.C <= 0f)
            {
                Debug.LogWarning($"{name}: у компонента «{comp.name}» C <= 0 — печь его не греет", this);
            }

            if (comp.tOptLow > comp.tOptHigh)
            {
                Debug.LogWarning($"{name}: у компонента «{comp.name}» tOptLow > tOptHigh", this);
            }

            if (comp.tOptHigh > comp.tMax)
            {
                Debug.LogWarning($"{name}: у компонента «{comp.name}» окно готовности выше tMax — уровень непроходим", this);
            }

            float rate = comp.mu;
            for (int j = 0; j < n; j++) rate += H(i, j);

            if (rate * dt > 0.5f)
            {
                Debug.LogWarning(
                    $"{name}: у компонента «{comp.name}» μ + Σh = {rate:F2} слишком велико для шага {dt:F4} с — " +
                    "явный интегратор начнёт врать. Подними simRate или снизь коэффициенты", this);
            }
        }
    }
}
