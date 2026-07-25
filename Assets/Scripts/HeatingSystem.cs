using UnityEngine;
using Zenject;

public class HeatingSystem : MonoBehaviour
{
    [SerializeField] private Dish dish;
    [SerializeField] private MeshRenderer microwaveBodyRenderer;
    [SerializeField] private Texture2D dirtyTexture;
    [SerializeField] private float dishRotationSpeed = 5f;

    [Inject] private MicrowaveContext _context;

    private MicrowaveTimer _microwaveTimer;

    public Dish Dish
    {
        set
        {
            dish = value;
            _context.Channels.Value = dish ? dish.Channels : null;
            dish.OnExplosion += PlayExplosionEffects;
        }
    }

    void Awake()
    {
        _microwaveTimer = GetComponent<MicrowaveTimer>();
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
        // if (dish.DishStatus != DishStatus.InProgress) return;
        
        dish.HeatComponents(Time.deltaTime);
        
        /*if (dish.DishStatus == DishStatus.Exploded)
        {
            _microwaveTimer.FinishHeating();
        }*/
    }

    private void CalculateCooling()
    {
        dish.CoolComponents(Time.deltaTime);
    }
}
