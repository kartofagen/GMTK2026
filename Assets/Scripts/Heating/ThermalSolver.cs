using UnityEngine;

/// <summary>
/// Тепловая модель блюда. Чистый C# без MonoBehaviour — считает только температуры,
/// ничего не знает ни про сцену, ни про эффекты.
///
///   dT_i/dt = u·k_i(T_i)·P·(w_i/Σw)/C_i − μ_i·(T_i − tEnv) + Σ_j h_ij·(T_j − T_i)
///
/// Постоянная часть правой части (матрица связей A и вклад среды c) раскладывается
/// один раз в конструкторе, в шаге остаётся только член нагрева, зависящий от u и k_i.
/// Остывание отдельной ветки не имеет: это то же уравнение при u = 0.
/// </summary>
public class ThermalSolver
{
    private readonly LevelConfig _level;
    private readonly int _n;

    // Кривые кэшируем: пустая (без ключей) Evaluate возвращает 0, то есть блюдо просто
    // не грелось бы. Такую трактуем как ровный нагрев k ≡ 1.
    private readonly AnimationCurve[] _curves;

    // A[i*n + j] — матрица связей: вне диагонали h_ij, на диагонали −μ_i − Σ_j h_ij.
    private readonly float[] _a;

    // b[i] — прирост dT_i/dt при u = 1 и k_i = 1.
    private readonly float[] _b;

    // c[i] — свободный член: вклад среды μ_i·tEnv.
    private readonly float[] _c;

    private readonly float[] _t;
    private readonly float[] _dt;

    public ThermalSolver(LevelConfig level)
    {
        _level = level;
        _n = level.ComponentCount;

        _a = new float[_n * _n];
        _b = new float[_n];
        _c = new float[_n];
        _t = new float[_n];
        _dt = new float[_n];
        _curves = new AnimationCurve[_n];

        // Σw — нормировка долей мощности. Если все веса нулевые, печь ничего не греет.
        float wSum = 0f;
        for (int i = 0; i < _n; i++) wSum += Mathf.Max(0f, level.Components[i].w);

        for (int i = 0; i < _n; i++)
        {
            var comp = level.Components[i];

            // Диагональ: собственная теплоотдача −μ_i плюс отток по всем связям.
            float diag = -comp.mu;
            for (int j = 0; j < _n; j++)
            {
                if (i == j) continue;

                float hij = level.H(i, j);
                _a[i * _n + j] = hij;
                diag -= hij;
            }

            _a[i * _n + i] = diag;

            // Мощность делится по весам; C_i переводит «ватты» в градусы в секунду.
            float share = wSum > 0f ? Mathf.Max(0f, comp.w) / wSum : 0f;
            _b[i] = comp.C > 0f ? level.P * share / comp.C : 0f;

            // −μ_i(T_i − tEnv) = −μ_i·T_i + μ_i·tEnv; вторая часть уходит в свободный член.
            _c[i] = comp.mu * level.tEnv;

            _t[i] = comp.t0;

            _curves[i] = comp.heatCurve != null && comp.heatCurve.length > 0 ? comp.heatCurve : null;
        }
    }

    public int Count => _n;

    public float this[int i] => _t[i];

    /// <summary>
    /// Один шаг явным методом Эйлера. Шаг фиксированный (см. LevelConfig.simRate),
    /// поэтому траектория не зависит от FPS. Без аллокаций — вызывается 60 раз в секунду.
    /// </summary>
    public void Step(float u, float dt)
    {
        for (int i = 0; i < _n; i++)
        {
            float acc = _c[i];

            if (u != 0f && _b[i] != 0f)
            {
                // Кривая — доля отдачи печи. Значение вне [0,1] означало бы либо охлаждение
                // печью, либо мощность больше номинальной, поэтому зажимаем.
                float k = _curves[i] != null ? Mathf.Clamp01(_curves[i].Evaluate(_t[i])) : 1f;
                acc += u * _b[i] * k;
            }

            int row = i * _n;
            for (int j = 0; j < _n; j++) acc += _a[row + j] * _t[j];

            _dt[i] = acc;
        }

        // Производные считаются по состоянию на начало шага, поэтому запись — отдельным проходом.
        for (int i = 0; i < _n; i++) _t[i] += dt * _dt[i];
    }

    /// <summary>Первый компонент, пробивший свой потолок tMax, если такой есть.</summary>
    public bool TryGetViolation(out int index)
    {
        for (int i = 0; i < _n; i++)
        {
            if (_t[i] > _level.Components[i].tMax)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    /// <summary>Все ли компоненты сейчас внутри своих целевых окон.</summary>
    public bool AllInTargetWindow()
    {
        for (int i = 0; i < _n; i++)
        {
            var comp = _level.Components[i];
            if (_t[i] < comp.tOptLow || _t[i] > comp.tOptHigh) return false;
        }

        return _n > 0;
    }
}
