using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;

public class ResultText : MonoBehaviour
{
    private TextMeshProUGUI _timerText;
    [SerializeField] private Transform microwave;
    
    private Dictionary<DishStatus, KeyValuePair<string, Color>> resultDict = new()
    {
        {DishStatus.Success, new KeyValuePair<string, Color>("SUCCESS!", Color.green)},
        {DishStatus.Exploded, new KeyValuePair<string, Color>("EXPLODED!", Color.red)},
        {DishStatus.Overheating, new KeyValuePair<string, Color>("THIS HEATING SUCKS!", Color.orange)},
        {DishStatus.Underheating, new KeyValuePair<string, Color>("THIS HEATING SUCKS!", Color.orange)}
    };
    
    private HeatingSystem _heatingSystem;
    private MovingInsideSystem _movingInsideSystem;
    private MicrowaveTimer _timer;
    
    private void Awake()
    {
        _timerText = GetComponent<TextMeshProUGUI>();
        
        _heatingSystem = microwave.GetComponent<HeatingSystem>();
        _movingInsideSystem = microwave.GetComponent<MovingInsideSystem>();
        _timer = microwave.GetComponent<MicrowaveTimer>();
        
        _heatingSystem
            .onHeatingFinished
            .Subscribe(PrintResult)
            .AddTo(this);
        
        _movingInsideSystem
            .onDishOutside
            .Subscribe(HideResult)
            .AddTo(this);
        
        _timer
            .onStarted
            .Subscribe(HideResult)
            .AddTo(this);
    }

    private void PrintResult(DishStatus status)
    {
        _timerText.text = resultDict[status].Key;
        _timerText.color = resultDict[status].Value;
        _timerText.enabled = true;
    }
    
    private void HideResult(Unit unit) => _timerText.enabled = false;
}
