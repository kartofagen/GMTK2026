using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;

public class ResultText : MonoBehaviour
{
    private TextMeshProUGUI _timerText;
    [SerializeField] private HeatingSystem microwave;
    
    private Dictionary<DishStatus, KeyValuePair<string, Color>> resultDict = new()
    {
        {DishStatus.Success, new KeyValuePair<string, Color>("SUCCESS!", Color.green)},
        {DishStatus.Exploded, new KeyValuePair<string, Color>("EXPLODED!", Color.red)},
        {DishStatus.BadHeating, new KeyValuePair<string, Color>("THIS HEATING SUCKS!", Color.orange)},
        
    };
    
    private void Awake()
    {
        _timerText = GetComponent<TextMeshProUGUI>();
        
        microwave
            .onHeatingFinished
            .Subscribe(PrintResult)
            .AddTo(this);
    }

    private void PrintResult(DishStatus status)
    {
        Debug.Log("asdfasdf");
        _timerText.enabled = true;
        _timerText.text = resultDict[status].Key;
        _timerText.color = resultDict[status].Value;
    }
}
