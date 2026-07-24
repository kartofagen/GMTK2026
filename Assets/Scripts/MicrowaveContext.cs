using System.Collections.Generic;
using R3;

/// <summary>
/// Реактивный набор активных температурных каналов (какие серии сейчас показывать).
/// Кто греется — тот и кладёт сюда свои каналы: реальное блюдо или тестовый источник.
/// График подписывается и пересобирает серии при смене набора.
/// </summary>
public class MicrowaveContext
{
    public ReactiveProperty<IReadOnlyList<ITemperatureChannel>> Channels { get; } = new();
}
