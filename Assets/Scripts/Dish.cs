using System;
using System.Collections.Generic;
using UnityEngine;

public enum DishStatus
{
    InProgress,
    Success,
    BadHeating,
    Exploded
}

public class Dish : MonoBehaviour
{
    public string dishName;
    [SerializeField] private DishComponent[] components;

    public DishStatus DishStatus { get; private set; } = DishStatus.InProgress;

    public string DishName => dishName;

    /// <summary>Каналы температуры по компонентам — по одному на серию графика.</summary>
    public IReadOnlyList<ITemperatureChannel> Channels => components;
    
    public int Index { get; set; }

    private DishMovement _dishMovement;

    void Awake()
    {
        _dishMovement = GetComponent<DishMovement>();
    }

    public event Action OnExplosion;

    public void HeatComponents(float deltaTime)
    {
        foreach (var component in components)
        {
            component.Heat(deltaTime);
        }
        UpdateStatus();
    }
    
    public void CoolComponents(float deltaTime)
    {
        float averageTemp = 0f;
        foreach (var component in components)
        {
            averageTemp += component.CurrentTemp;
        }
        averageTemp /= components.Length;

        foreach (var component in components)
        {
            component.Cool(deltaTime, averageTemp);
        }
    }

    private void UpdateStatus()
    {
        foreach (var component in components)
        {
            var status = component.GetStatus();
            if (status == DishComponentStatus.Explodes)
            {
                DishStatus = DishStatus.Exploded;
                OnExplosion?.Invoke();
                Debug.Log("Explode!!!");
                return;
            }
        }
    }

    public DishStatus GetFinalStatus()
    {
        if (DishStatus != DishStatus.Exploded)
        {
            foreach (var component in components)
            {
                var status = component.GetStatus();
                if (status != DishComponentStatus.Ready)
                {
                    DishStatus = DishStatus.BadHeating;
                    break;
                }
            }
            if (DishStatus != DishStatus.BadHeating) DishStatus = DishStatus.Success;
        }
        return DishStatus;
    }

    public void Reset()
    {
        DishStatus = DishStatus.InProgress;
        _dishMovement.MovementState = DishMovementState.Idle;
        foreach (var component in components)
        {
            component.Reset();
        }
    }
}
