using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using XCharts.Runtime;
using Zenject;

public class TemperatureChart : MonoBehaviour
{
    [SerializeField] private LineChart chart;
    [SerializeField, Tooltip("Заглушка \"Choose a dish\" — показывается, пока каналов нет")]
    private GameObject placeholder;
    [SerializeField] private float sampleInterval = 0.5f;
    [SerializeField] private int maxPoints = 20; // ширина окна прокрутки (кол-во точек)

    [Inject] private MicrowaveContext _context;
    [Inject] private GameConfig _gameConfig;

    private IReadOnlyList<ITemperatureChannel> _channels;
    private int _sampleIndex;

    // Яркая палитра линий (Material A200/A400), циклится по каналам.
    private static readonly Color32[] Palette =
    {
        new Color32(0xFF, 0x52, 0x52, 0xFF), // красный
        new Color32(0x40, 0xC4, 0xFF, 0xFF), // голубой
        new Color32(0x69, 0xF0, 0xAE, 0xFF), // зелёный
        new Color32(0xFF, 0xD7, 0x40, 0xFF), // жёлтый
        new Color32(0xE0, 0x40, 0xFB, 0xFF), // фиолетовый
        new Color32(0xFF, 0x6E, 0x40, 0xFF), // оранжевый
    };

    void Start()
    {
        SetupCosmetics();

        // X — категория (метка времени), Y — значение температуры.
        // Скольжение ("бегущая ЭКГ") работает именно на Category-оси: при переполнении
        // maxCache старая категория и старая точка удаляются, окно едет влево.
        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.boundaryGap = true;
        xAxis.maxCache = maxPoints;

        chart.RemoveData();

        // Пересобираем серии при смене набора активных каналов (сменилось блюдо / тестовый источник).
        // null / пусто -> показываем заглушку "Choose a dish". Подписка сразу отдаёт текущее
        // значение (стартом это null), так что начальное состояние выставится само.
        _context.Channels
        .Subscribe(OnChannelsChanged)
        .AddTo(this);

        // Единый таймер сэмплирования — все серии тикают синхронно, X-оси совпадают.
        Observable
        .Interval(TimeSpan.FromSeconds(sampleInterval), UnityTimeProvider.Update)
        .Subscribe(_ => Sample())
        .AddTo(this); // авто-отписка при Destroy
    }

    private void SetupCosmetics()
    {
        var title = chart.EnsureChartComponent<Title>();
        title.show = true;
        title.text = "Dish Components Temperature";
        StyleText(title.labelStyle.textStyle);

        var xAxis = chart.EnsureChartComponent<XAxis>();
        xAxis.axisName.name = "Time, sec";
        xAxis.axisName.show = true;
        StyleText(xAxis.axisName.labelStyle.textStyle);
        StyleText(xAxis.axisLabel.textStyle);
        // По центру под осью, опущено на 20px (положительный y здесь двигает вниз).
        xAxis.axisName.labelStyle.position = LabelStyle.Position.Middle;
        xAxis.axisName.labelStyle.offset = new Vector3(0f, -50f, 0f);

        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.axisName.name = "T, °C";
        yAxis.axisName.show = true;
        StyleText(yAxis.axisName.labelStyle.textStyle);
        StyleText(yAxis.axisLabel.textStyle);

        // Фиксация оси Y
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        yAxis.min = 0f;
        yAxis.max = _gameConfig.explosionThreshold + 10f;

        // Нижнее поле сетки — место под подпись оси X и легенду.
        chart.EnsureChartComponent<GridCoord>().bottom = 70f;

        // Легенда снизу: квадратная плашка цвета линии + имя компонента.
        // Цвет плашки XCharts берёт из серии автоматически (мы задаём его в RebuildSeries).
        var legend = chart.EnsureChartComponent<Legend>();
        legend.show = true;
        legend.orient = Orient.Horizonal;
        legend.iconType = Legend.Type.Rect;
        legend.location = Location.defaultBottom;
        StyleText(legend.labelStyle.textStyle);

        // Опускаем легенду на 50px ниже дефолта. location.bottom при значении <=1 —
        // это доля высоты, поэтому переводим 50px в долю (может уйти в минус — ниже рамки).
        float chartH = chart.chartHeight > 1f
        ? chart.chartHeight
        : ((RectTransform)chart.transform).rect.height;
        legend.location.bottom -= 40f / Mathf.Max(chartH, 1f);

        // Прозрачный фон: Background с show=true, но своим цветом с alpha=0.
        // Тогда theme.GetBackgroundColor вернёт этот прозрачный цвет вместо цвета темы.
        var background = chart.EnsureChartComponent<Background>();
        background.show = true;
        background.autoColor = false;
        background.imageColor = new Color(0f, 0f, 0f, 0f);
    }

    // Белый жирный текст. autoColor=false, иначе XCharts подставит свой контрастный цвет.
    private static void StyleText(TextStyle textStyle)
    {
        textStyle.show = true;
        textStyle.autoColor = false;
        textStyle.color = Color.white;
        textStyle.fontStyle = FontStyle.Bold;
    }

    private void OnChannelsChanged(IReadOnlyList<ITemperatureChannel> channels)
    {
        bool hasChannels = channels != null && channels.Count > 0;

        if (placeholder) placeholder.SetActive(!hasChannels);
        chart.gameObject.SetActive(hasChannels);

        if (!hasChannels)
        {
            _channels = null;
            chart.RemoveData();
            return;
        }

        RebuildSeries(channels);
    }

    private void RebuildSeries(IReadOnlyList<ITemperatureChannel> channels)
    {
        _channels = channels;

        chart.RemoveData();
        for (int i = 0; i < channels.Count; i++)
        {
            var serie = chart.AddSerie<Line>(channels[i].Name);
            serie.maxCache = maxPoints;

            
            //serie.symbol.show = false; // Нет точек
            serie.symbol.size = 3f; // Размер точек

            var color = Palette[i % Palette.Length];
            serie.lineStyle.color = color;   // цвет самой линии
            serie.itemStyle.color = color;   // точки/маркеры/легенда
        }
        
        DrawHorizontalArea(0, chart.series.ToList()[0],
            _gameConfig.targetTempRange.x, _gameConfig.targetTempRange.y,
            Color.green);
        DrawHorizontalArea(1, chart.series.ToList()[0],
            _gameConfig.explosionThreshold, _gameConfig.explosionThreshold + 20f,
            Color.red);

        _sampleIndex = 0;
    }
    
    private void DrawHorizontalArea(int areaIndex, Serie targetSerie, float low, float high, Color color)
    {
        var markArea = chart.GetChartComponentNum<MarkArea>() > areaIndex
            ? chart.GetChartComponent<MarkArea>(areaIndex)
            : chart.AddChartComponent<MarkArea>();

        markArea.show = true;
        markArea.serieIndex = targetSerie.index;
        markArea.start.type = MarkAreaType.None;
        markArea.start.yValue = low;
        markArea.end.type = MarkAreaType.None;
        markArea.end.yValue = high;
        markArea.itemStyle.color = color;
        markArea.itemStyle.opacity = 0.12f;
    }

    private void Sample()
    {
        if (_channels == null) return;

        // Метка = номер сэмпла * шаг. Детерминированно, без дрейфа Time.time:
        // при 0.5с -> 0.5, 1, 1.5, 2 ... (без дублей и пропусков).
        _sampleIndex++;
        // Всегда один знак после запятой (0.5, 1.0, 1.5, 2.0 ...), чтобы метки
        // выглядели единообразно — иначе "0.#" ронял нули у целых.
        float seconds = _sampleIndex * sampleInterval;
        chart.AddXAxisData(seconds.ToString("0.0"));

        for (int i = 0; i < _channels.Count; i++)
        {
            chart.AddData(i, _channels[i].Temperature.CurrentValue);
        }
    }
}
