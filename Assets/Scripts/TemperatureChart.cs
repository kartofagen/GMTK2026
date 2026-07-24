using System;
using R3;
using UnityEngine;
using XCharts.Runtime;
using Zenject;

public class TemperatureChart : MonoBehaviour
{
    [SerializeField] private LineChart chart;
    [SerializeField] private float sampleInterval = 0.5f;
    [SerializeField] private int maxPoints = 5; // окно прокрутки

    [Inject] private TemperatureModel _model;
    private float _start;

    void Start()
    {
        _start = Time.time;
        chart.ClearData();

        var serie = chart.AddSerie<Line>("Температура");
        serie.maxCache = maxPoints; // держим последние N точек, график едет вправо

        Observable
            .Interval(TimeSpan.FromSeconds(sampleInterval), UnityTimeProvider.Update)
            .Subscribe(_ =>
            {
                float t = Time.time - _start;
                chart.AddData(0, t, _model.Temperature.CurrentValue);
            })
            .AddTo(this); // авто-отписка при Destroy
    }
}
