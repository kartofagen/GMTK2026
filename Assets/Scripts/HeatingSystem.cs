using System;
using R3;
using UnityEngine;
using Zenject;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    [SerializeField] private MeshRenderer microwaveBodyRenderer;
    [SerializeField] private Texture2D dirtyTexture;
    [SerializeField] private float dishRotationSpeed = 5f;
    [SerializeField] private GameObject smokeEffect;

    [Inject] private MicrowaveContext _context;

    private MicrowaveTimer _microwaveTimer;
    
    public readonly Subject<DishStatus> onHeatingFinished = new();

    public Dish Dish
    {
        get => dish;
        set
        {
            dish = value;
            _context.Channels.Value = dish ? dish.Channels : null;
            if (dish) dish.OnExplosion += PlayExplosionEffects;
        }
    }

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();
        
        _microwaveTimer
            .onFinished
            .Subscribe(OnFinished)
            .AddTo(this);
    }

    void Start()
    {
        // Блюдо могло быть задано в инспекторе, а не через сеттер.
        if (dish) _context.Channels.Value = dish.Channels;
    }

    void Update()
    {
        if (!dish) return;
        if (_microwaveTimer.State == MicrowaveState.Finished) return;
        
        switch (_microwaveTimer.State)
        {
            case MicrowaveState.Heating:
                CalculateHeating();
                dish.transform.Rotate(Vector3.up, dishRotationSpeed * Time.deltaTime); 
                break;
            case MicrowaveState.Paused:
                CalculateCooling();
                break;
        }
    }

    private void PlayExplosionEffects()
    {
        microwaveBodyRenderer.material.SetTexture("_BaseMap", dirtyTexture);
    }

    private void CalculateHeating()
    {
        /*if (dish.DishStatus == DishStatus.Exploded)
        {
            _microwaveTimer.FinishHeating();
        }*/
        
        dish.HeatComponents(Time.deltaTime);
    }

    private void CalculateCooling()
    {
        dish.CoolComponents(Time.deltaTime);
    }

    private void OnFinished(Unit unit)
    {
        if (!dish) return;
        
        onHeatingFinished.OnNext(dish.GetFinalStatus());
    }
}
