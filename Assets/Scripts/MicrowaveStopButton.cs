using System;
using UnityEngine;

public class MicrowaveStopButton : MicrowaveButton
{
    [SerializeField] private MicrowaveTimer microwaveTimer;

    protected override void Action()
    {
        microwaveTimer.PauseHeating();
    }
}
