using System.Collections.Generic;
using R3;

/// <summary>
/// Реактивный набор активных температурных каналов (какие серии сейчас показывать)
/// и конфиг уровня, из которого они считаются.
/// Кто греется — тот и кладёт сюда свои каналы: реальное блюдо или тестовый источник.
/// График подписывается и пересобирает серии при смене набора.
/// </summary>
public class MicrowaveContext
{
    public ReactiveProperty<IReadOnlyList<ITemperatureChannel>> Channels { get; } = new();

    /// <summary>Уровень загруженного блюда: пороги, окна и кулдаун кнопки берутся отсюда.</summary>
    public ReactiveProperty<LevelConfig> Level { get; } = new();
}
