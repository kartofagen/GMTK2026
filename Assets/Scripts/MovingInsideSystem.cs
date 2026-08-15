using DG.Tweening;
using R3;
using UnityEngine;

public class MovingInsideSystem : MonoBehaviour
{
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform targetPoint;
    
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeMove = Ease.InOutBack;
    
    [SerializeField] private DoorRotation door;
    
    [SerializeField] private DishesServingManager dishesServingManager;
    
    [Header("Finish")]
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Ease easeToMouth = Ease.InOutBack;
    
    [SerializeField] private Transform trashPoint;
    [SerializeField] private Ease easeToTrash = Ease.InOutBack;
    
    [SerializeField] private Transform hotPoint;
    [SerializeField] private Ease easeHot = Ease.InOutBack;
    
    [SerializeField] private Transform veryHotPoint;
    [SerializeField] private Ease easeVeryHot = Ease.InOutBack;
    
    [SerializeField] private float finishedDuration = 1f;
    
    public Transform EntryPoint => entryPoint;
    public Transform TargetPoint => targetPoint;
    
    private HeatingSystem _heatingSystem;

    private Transform _dish;
    private Sequence _movingInsideSeq;
    private Tween _floatingTween;
    
    private Sequence _movingOutsideSeq;
    
    public readonly Subject<Unit> onDishTouched = new();
    public readonly Subject<Unit> onDishOutside = new();
    
    private void Awake()
    {
        _heatingSystem = GetComponent<HeatingSystem>();
    }
    
    public void Register(Transform dish)
    {
        _dish = dish;

        if (door.IsOpened)
        {
            MoveInside();
        }
        else
        {
            door.DoorOpened += MoveInside;
            
            _floatingTween = _dish
                .DOMoveY(_dish.transform.position.y + 0.05f, 2f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(UpdateType.Normal);
        }
    }

    private void MoveInside()
    {
        if (!_dish) return;
        
        if (_floatingTween != null && _floatingTween.IsActive())
        {
            _floatingTween.Kill();
        }
        
        _movingInsideSeq = DOTween.Sequence();
        _movingInsideSeq.Append(_dish.DOMoveY(TargetPoint.position.y + 0.2f, 0.5f));
        _movingInsideSeq.Append(_dish.DOMove(TargetPoint.position, duration));
        _movingInsideSeq.SetEase(easeMove).Play();
        
        _dish.parent = transform;
        _heatingSystem.Dish = _dish.GetComponent<Dish>();
        _dish.GetComponent<DishMovement>().MovementState = DishMovementState.Inside;
        
        door.DoorOpened -= MoveInside;
    }

    public void MoveOutside()
    {
        _dish.parent = dishesServingManager.transform;
        
        _movingOutsideSeq = DOTween.Sequence();

        if (_heatingSystem.Dish.DishStatus == DishStatus.Exploded)
        {
            _movingOutsideSeq.AppendCallback(() => onDishTouched.OnNext(Unit.Default));
        }
        
        _movingOutsideSeq.Append(_dish.DOMoveY(_dish.position.y + 0.1f, 0.5f));
        _movingOutsideSeq.Append(_dish.DOMove(EntryPoint.position, duration));
        
        switch (_heatingSystem.Dish.DishStatus)
        {
            default:
                _movingOutsideSeq.Append(_dish.DOMove(trashPoint.position, finishedDuration).SetEase(easeToTrash));
                break;
            case DishStatus.Success:
                _movingOutsideSeq.Append(_dish.DOMove(mouthPoint.position, finishedDuration).SetEase(easeToMouth));
                _movingOutsideSeq.AppendCallback(() => onDishTouched.OnNext(Unit.Default));
                break;
            case DishStatus.Overheating:
                _movingOutsideSeq.Append(_dish.DOMove(mouthPoint.position, finishedDuration).SetEase(easeToMouth));
                _movingOutsideSeq.AppendCallback(() => onDishTouched.OnNext(Unit.Default));
                _movingOutsideSeq.Append(_dish.DOMove(hotPoint.position, finishedDuration).SetEase(easeHot));
                _movingOutsideSeq.Join(_dish.DOLocalRotate(hotPoint.localEulerAngles, finishedDuration).SetEase(Ease.InOutBack));
                break;
            case DishStatus.Exploded:
                _movingOutsideSeq.Append(_dish.DOMove(veryHotPoint.position, finishedDuration / 2).SetEase(easeVeryHot));
                break;
        }
        _movingOutsideSeq.AppendCallback(() => onDishOutside.OnNext(Unit.Default));
        
        _heatingSystem.Dish = null;
        
        _movingOutsideSeq.SetEase(easeMove).Play().OnComplete(() => dishesServingManager.RemoveDish(_dish));
    }
}
