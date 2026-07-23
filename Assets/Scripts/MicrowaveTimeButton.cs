using System;
using UnityEngine;

public class MicrowaveTimeButton : MicrowaveButton
{
    [SerializeField] private int deltaTime = 1;
    [SerializeField] private MicrowaveTimer microwaveTimer;
    
    protected override void Action()
    {
        microwaveTimer.AddTime(deltaTime);
    }
}
