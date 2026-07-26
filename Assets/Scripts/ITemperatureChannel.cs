using R3;

/// <summary>
/// Именованный источник температуры (один компонент блюда, тестовый генератор и т.п.).
/// Одна серия на графике = один канал.
/// </summary>
public interface ITemperatureChannel
{
    string Name { get; }
    ReadOnlyReactiveProperty<float> Temperature { get; }

    /// <summary>
    /// Канал больше не отдаёт осмысленную температуру: продукт взорвался. Значения
    /// продолжают идти (симуляция не останавливается), но рисовать их уже нельзя.
    /// </summary>
    ReadOnlyReactiveProperty<bool> Stopped { get; }
}
