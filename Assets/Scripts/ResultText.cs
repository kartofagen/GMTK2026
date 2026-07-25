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
        {DishStatus.BadHeating, new KeyValuePair<string, Color>("THIS HEATING SUCKS!", Color.orange)},
        
    };
    
    private HeatingSystem _heatingSystem;
    private MovingInsideSystem _movingInsideSystem;
    
    private void Awake()
    {
        _timerText = GetComponent<TextMeshProUGUI>();
        
        _heatingSystem = microwave.GetComponent<HeatingSystem>();
        _movingInsideSystem = microwave.GetComponent<MovingInsideSystem>();
        
        _heatingSystem
            .onHeatingFinished
            .Subscribe(PrintResult)
            .AddTo(this);
        
        _movingInsideSystem
            .onMovingOutside
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
