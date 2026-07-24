using R3;

/// <summary>
/// Именованный источник температуры (один компонент блюда, тестовый генератор и т.п.).
/// Одна серия на графике = один канал.
/// </summary>
public interface ITemperatureChannel
{
    string Name { get; }
    ReadOnlyReactiveProperty<float> Temperature { get; }
}
