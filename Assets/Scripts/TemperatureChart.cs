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
    [SerializeField]
    private GameObject doorPlaceholder;
    [SerializeField, Tooltip("Дверь микроволновки — пока она открыта, а блюдо внутри, график прячется за DoorPlaceholder")]
    private DoorRotation door;
    [SerializeField] private float sampleInterval = 0.5f;
    [SerializeField] private int maxPoints = 20; // ширина окна прокрутки (кол-во точек)

    [SerializeField, Range(0f, 0.9f), Tooltip("Доля ширины справа, которую график оставляет пустой " +
    "(график сдвинут влево на эту долю)")]
    private float rightPadFraction = 0.25f;

    [SerializeField, Tooltip("Верх оси Y, пока блюдо не загружено и потолки компонентов неизвестны")]
    private float fallbackMaxTemp = 110f;

    [SerializeField, Tooltip("Толщина пунктира максимальной температуры компонента")]
    private float ceilingLineWidth = 3f;

    [SerializeField, Range(0f, 1f), Tooltip("Прозрачность заливки окна оптимума. Домножается " +
    "на альфу цвета компонента из LevelConfig")]
    private float optimumAreaOpacity = 0.2f;

    [Inject] private MicrowaveContext _context;

    private IReadOnlyList<ITemperatureChannel> _channels;
    private IReadOnlyList<ITemperatureChannel> _lastRebuiltChannels;
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
        // Дверь примешана тем же потоком: пока блюдо внутри, но дверь открыта, график прячется
        // за DoorPlaceholder — CombineLatest пересчитывает видимость на любое из двух изменений,
        // а RebuildSeries вызывается отдельно, только когда реально сменился набор каналов.
        Observable
        .CombineLatest(_context.Channels, door.IsOpenedProperty, (channels, isDoorOpen) => (channels, isDoorOpen))
        .Subscribe(t => OnStateChanged(t.channels, t.isDoorOpen))
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
        
        // Точность значений в подсказке. itemFormatter — строковый шаблон XCharts,
        // а не делегат: {.} — точка цвета серии, {a} — имя серии, {c} — значение Y,
        // которое numericFormatter печатает с одним знаком после запятой.
        var tooltip = chart.EnsureChartComponent<Tooltip>();
        tooltip.show = true;
        tooltip.itemFormatter = "{.}|{a}|{c}";
        tooltip.numericFormatter = "F1";
    }

    // Белый жирный текст. autoColor=false, иначе XCharts подставит свой контрастный цвет.
    private static void StyleText(TextStyle textStyle)
    {
        textStyle.show = true;
        textStyle.autoColor = false;
        textStyle.color = Color.white;
        textStyle.fontStyle = FontStyle.Bold;
    }

    private void OnStateChanged(IReadOnlyList<ITemperatureChannel> channels, bool isDoorOpen)
    {
        bool hasChannels = channels != null && channels.Count > 0;

        // Три взаимоисключающих состояния:
        // - нет блюда -> "Choose a dish";
        // - блюдо внутри, дверь открыта -> DoorPlaceholder, график спрятан (но не сброшен);
        // - блюдо внутри, дверь закрыта -> сам график.
        if (placeholder) placeholder.SetActive(!hasChannels);
        if (doorPlaceholder) doorPlaceholder.SetActive(hasChannels && isDoorOpen);
        chart.gameObject.SetActive(hasChannels && !isDoorOpen);

        if (!hasChannels)
        {
            _channels = null;
            _lastRebuiltChannels = null;
            chart.RemoveData();
            return;
        }

        // Открытие/закрытие двери переиспользует тот же набор каналов — пересборка нужна
        // только когда канал реально сменился (новое блюдо), иначе история на графике
        // сбрасывалась бы каждый раз, когда просто хлопают дверцей.
        if (!ReferenceEquals(channels, _lastRebuiltChannels))
        {
            RebuildSeries(channels);
            _lastRebuiltChannels = channels;
        }
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

        // Цвет компонента общий для всей его графики: линия температуры, окно оптимума
        // и пунктир потолка — чтобы взглядом связывать пороги с их продуктом.
        var colors = new Color[channels.Count];
        for (int i = 0; i < channels.Count; i++)
        {
            var serie = chart.AddSerie<Line>(channels[i].Name);
            serie.maxCache = SeriePoints;


            //serie.symbol.show = false; // Нет точек
            serie.symbol.size = 3f; // Размер точек

            Color color = Palette[i % Palette.Length];
            if (hasLevel && level.Components[i].color.a > 0f) color = level.Components[i].color;
            colors[i] = color;

            serie.lineStyle.color = color;   // цвет самой линии
            serie.itemStyle.color = color;   // точки/маркеры/легенда
        }

        var series = chart.series.ToList();
        var yAxis = chart.EnsureChartComponent<YAxis>();
        yAxis.max = fallbackMaxTemp;

        // Пороги от предыдущего блюда переиспользуются по индексу; лишние гасим — иначе
        // на блюде с меньшим числом компонентов останутся висеть чужие. Делать это надо
        // до любого выхода из метода: MarkLineHandler разыменовывает серию раньше, чем
        // проверяет show, поэтому «висячий» serieIndex роняет отрисовку графика с NRE.
        HideExtraMarks(series.Count);

        if (!hasLevel || series.Count == 0) return;

        // Пороги пер-компонентные: у каждого продукта своё окно готовности и свой потолок,
        // поэтому и полос, и пунктиров ровно столько же, сколько серий.
        float maxCeiling = 0f;
        for (int i = 0; i < series.Count; i++)
        {
            var comp = level.Components[i];

            DrawHorizontalArea(i, series[i], comp.tOptLow, comp.tOptHigh, colors[i]);
            DrawHorizontalLine(i, series[i], comp.tMax, colors[i]);

            maxCeiling = Mathf.Max(maxCeiling, comp.tMax);
        }

        yAxis.max = maxCeiling + 10f;
    }

    // Гасим пороги, которым уже не соответствует ни одна серия. Кроме show=false
    // обязательно переводим serieIndex на существующую серию: XCharts берёт серию по
    // индексу и обращается к ней до проверки show, так что индекс удалённой серии
    // означает NullReferenceException каждый кадр.
    private void HideExtraMarks(int serieCount)
    {
        for (int i = serieCount; i < chart.GetChartComponentNum<MarkArea>(); i++)
        {
            var markArea = chart.GetChartComponent<MarkArea>(i);
            markArea.show = false;
            markArea.serieIndex = 0;
        }
        for (int i = serieCount; i < chart.GetChartComponentNum<MarkLine>(); i++)
        {
            var markLine = chart.GetChartComponent<MarkLine>(i);
            markLine.show = false;
            markLine.serieIndex = 0;
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
        markArea.itemStyle.opacity = optimumAreaOpacity;
    }

    // Потолок компонента — жирный пунктир поперёк всей сетки, в цвете самого компонента.
    private void DrawHorizontalLine(int lineIndex, Serie targetSerie, float value, Color color)
    {
        var markLine = chart.GetChartComponentNum<MarkLine>() > lineIndex
            ? chart.GetChartComponent<MarkLine>(lineIndex)
            : chart.AddChartComponent<MarkLine>();

        markLine.show = true;
        markLine.serieIndex = targetSerie.index;

        // AddChartComponent кладёт в новый MarkLine дефолтную линию "average" — вычищаем
        // и держим ровно один элемент, чтобы переиспользование по индексу не копило мусор.
        if (markLine.data.Count != 1)
        {
            markLine.data.Clear();
            markLine.data.Add(new MarkLineData());
        }

        var data = markLine.data[0];
        data.type = MarkLineType.Custom;
        data.yValue = value;                 // горизонталь на уровне потолка
        data.lineStyle.type = LineStyle.Type.Dashed;
        data.lineStyle.width = ceilingLineWidth;
        data.lineStyle.color = color;
        data.startSymbol.show = false;
        data.endSymbol.show = false;
        data.label.show = false;
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
            var point = chart.AddData(i, _channels[i].Temperature.CurrentValue);

            // Взорвавшийся продукт: точку всё равно добавляем, но помечаем ignore.
            // Пропускать её нельзя — точка серии жёстко ложится в слот категории по своему
            // индексу, и отставшая серия «прилипла» бы к сетке, пока остальные едут влево.
            // Помеченная точка не рисуется и рвёт линию: график компонента замирает на
            // моменте взрыва и уезжает влево вместе со всеми.
            if (point != null && _channels[i].Stopped.CurrentValue) point.ignore = true;
        }
    }
}
