using UnityEngine;
using XCharts.Runtime;

public class ChartExample : MonoBehaviour
{
    public LineChart lineChart;

    void Start()
    {
        lineChart.ClearData();

        lineChart.AddSerie<Line>("Серия1");

        for (int i = 0; i < 50; i++)
        {
            lineChart.AddXAxisData("Точка " + i);
            lineChart.AddData(0, Random.Range(10, 100));
        }
    }
}