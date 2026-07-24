using System;
using R3;
using UnityEngine;
using XCharts.Runtime;
using Zenject;

public class TemperatureChart : MonoBehaviour
{
    [SerializeField] private LineChart chart;
    [SerializeField] private float sampleInterval = 0.5f;
    [SerializeField] private int maxPoints = 20; // ширина окна прокрутки (кол-во точек)

    [Inject] private TemperatureModel _model;
    private float _start;

    void Start()
    {
        _start = Time.time;

        // X — категория (метка времени), Y — значение температуры.
        // Скольжение ("бегущая ЭКГ") работает именно на Category-оси: при переполнении
        // maxCache старая категория и старая точка удаляются, окно едет влево.
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.boundaryGap = true;
        xAxis.maxCache = maxPoints;

        chart.RemoveData();

        var serie = chart.AddSerie<Line>("Температура");
        serie.maxCache = maxPoints;

        Observable
            .Interval(TimeSpan.FromSeconds(sampleInterval), UnityTimeProvider.Update)
            .Subscribe(_ =>
            {
                float t = Time.time - _start;
                chart.AddXAxisData(t.ToString("F1"));
                chart.AddData(0, _model.Temperature.CurrentValue);
            })
            .AddTo(this); // авто-отписка при Destroy
    }
}
