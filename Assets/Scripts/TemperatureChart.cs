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

    [SerializeField, Range(0f, 0.9f), Tooltip("Доля ширины справа, которую график оставляет пустой " +
    "(график сдвинут влево на эту долю)")]
    private float rightPadFraction = 0.25f;

    [SerializeField, Tooltip("Верх оси Y, пока блюдо не загружено и потолки компонентов неизвестны")]
    private float fallbackMaxTemp = 110f;

    [Inject] private MicrowaveContext _context;

    private IReadOnlyList<ITemperatureChannel> _channels;
    private int _sampleIndex;

    // Сколько крайних правых слотов оси X остаются без точек. Ось всегда держит maxPoints
    // категорий, серии — на PadPoints меньше, поэтому линия упирается не в правый край,
    // а заканчивается за PadPoints делений до него.
    private int PadPoints => Mathf.Clamp(Mathf.RoundToInt(maxPoints * rightPadFraction), 0, maxPoints - 1);
    private int SeriePoints => maxPoints - PadPoints;

    // Запасная палитра линий (Material A200/A400) — на случай, если цвет в LevelConfig
    // не задан (прозрачный) или уровня нет вовсе, как в тестовом источнике каналов.
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

        // Фиксация оси Y. Верх уточняется в RebuildSeries по потолкам компонентов уровня —
        // здесь только значение по умолчанию, пока блюдо не загружено.
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        yAxis.min = 0f;
        yAxis.max = fallbackMaxTemp;

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

        var level = _context.Level.CurrentValue;
        // Уровень может не совпасть с набором каналов (тестовый источник) — тогда работаем
        // без порогов, на одной палитре.
        bool hasLevel = level != null && level.ComponentCount == channels.Count;

        chart.RemoveData();
        ResetSampling();

        for (int i = 0; i < channels.Count; i++)
        {
            var serie = chart.AddSerie<Line>(channels[i].Name);
            serie.maxCache = SeriePoints;


            //serie.symbol.show = false; // Нет точек
            serie.symbol.size = 3f; // Размер точек

            Color color = Palette[i % Palette.Length];
            if (hasLevel && level.Components[i].color.a > 0f) color = level.Components[i].color;

            serie.lineStyle.color = color;   // цвет самой линии
            serie.itemStyle.color = color;   // точки/маркеры/легенда
        }

        var series = chart.series.ToList();
        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.max = fallbackMaxTemp;

        if (!hasLevel || series.Count == 0) return;

        // Пороги пер-компонентные: у каждого продукта своё окно готовности и свой потолок,
        // поэтому полос столько же, сколько серий.
        int areaIndex = 0;
        float maxCeiling = 0f;
        for (int i = 0; i < series.Count; i++)
        {
            var comp = level.Components[i];

            DrawHorizontalArea(areaIndex++, series[i], comp.tOptLow, comp.tOptHigh, Color.green);
            DrawHorizontalArea(areaIndex++, series[i], comp.tMax, comp.tMax + 20f, Color.red);

            maxCeiling = Mathf.Max(maxCeiling, comp.tMax);
        }

        yAxis.max = maxCeiling + 10f;

        // Полосы от предыдущего блюда переиспользуются по индексу; лишние прячем,
        // иначе на менее «многолюдном» блюде останутся висеть чужие пороги.
        for (int i = areaIndex; i < chart.GetChartComponentNum<MarkArea>(); i++)
        {
            chart.GetChartComponent<MarkArea>(i).show = false;
        }
    }

    // Ось X стартует уже заполненной PadPoints метками будущего времени: серий на них нет,
    // поэтому пустая область справа есть с первого же сэмпла, а не только после того,
    // как окно прокрутки наберётся целиком.
    private void ResetSampling()
    {
        _sampleIndex = 0;
        for (int i = 1; i <= PadPoints; i++)
        {
            chart.AddXAxisData(FormatSeconds(i * sampleInterval));
        }
    }

    private static string FormatSeconds(float seconds)
    {
        // Всегда один знак после запятой (0.5, 1.0, 1.5, 2.0 ...), чтобы метки
        // выглядели единообразно — иначе "0.#" ронял нули у целых.
        return seconds.ToString("0.0");
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
        // Ось опережает серии на PadPoints, поэтому и метку добавляем на PadPoints шагов
        // вперёд — иначе подпись под точкой отставала бы от её реального времени.
        float seconds = (_sampleIndex + PadPoints) * sampleInterval;
        chart.AddXAxisData(FormatSeconds(seconds));

        for (int i = 0; i < _channels.Count; i++)
        {
            chart.AddData(i, _channels[i].Temperature.CurrentValue);
        }
    }
}
