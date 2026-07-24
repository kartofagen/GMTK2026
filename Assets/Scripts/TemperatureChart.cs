using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using XCharts.Runtime;
using Zenject;

public class TemperatureChart : MonoBehaviour
{
    [SerializeField] private LineChart chart;
    [SerializeField] private float sampleInterval = 0.5f;
    [SerializeField] private int maxPoints = 20; // ширина окна прокрутки (кол-во точек)

    [Inject] private MicrowaveContext _context;

    private IReadOnlyList<ITemperatureChannel> _channels;
    private float _start;

    void Start()
    {
        // X — категория (метка времени), Y — значение температуры.
        // Скольжение ("бегущая ЭКГ") работает именно на Category-оси: при переполнении
        // maxCache старая категория и старая точка удаляются, окно едет влево.
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.boundaryGap = true;
        xAxis.maxCache = maxPoints;

        chart.RemoveData();

        // Пересобираем серии при смене набора активных каналов (сменилось блюдо / тестовый источник).
        _context.Channels
            .Where(channels => channels != null)
            .Subscribe(RebuildSeries)
            .AddTo(this);

        // Единый таймер сэмплирования — все серии тикают синхронно, X-оси совпадают.
        Observable
            .Interval(TimeSpan.FromSeconds(sampleInterval), UnityTimeProvider.Update)
            .Subscribe(_ => Sample())
            .AddTo(this); // авто-отписка при Destroy
    }

    private void RebuildSeries(IReadOnlyList<ITemperatureChannel> channels)
    {
        _channels = channels;

        chart.RemoveData();
        foreach (var channel in channels)
        {
            var serie = chart.AddSerie<Line>(channel.Name);
            serie.maxCache = maxPoints;
        }

        _start = Time.time;
    }

    private void Sample()
    {
        if (_channels == null) return;

        float t = Time.time - _start;
        chart.AddXAxisData(t.ToString("F1"));

        for (int i = 0; i < _channels.Count; i++)
        {
            chart.AddData(i, _channels[i].Temperature.CurrentValue);
        }
    }
}
