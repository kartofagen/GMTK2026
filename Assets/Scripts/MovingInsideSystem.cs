using DG.Tweening;
using UnityEngine;

public class MovingInsideSystem : MonoBehaviour
{
    [SerializeField] private Transform entryPoint;
    [SerializeField] private Transform targetPoint;
    
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeMove = Ease.InOutBack;
    
    [SerializeField] private DoorRotation door;
    
    [Header("Finish")]
    [SerializeField] private Transform mouthPoint;
    [SerializeField] private Ease easeToMouth = Ease.InOutBack;
    
    [SerializeField] private Transform trashPoint;
    [SerializeField] private Ease easeToTrash = Ease.InOutBack;
    
    [SerializeField] private float finishedDuration = 1f;
    
    public Transform EntryPoint => entryPoint;
    public Transform TargetPoint => targetPoint;
    
    private HeatingSystem _heatingSystem;

    private Transform _dish;
    private Sequence _movingInsideSeq;
    private Tween _floatingTween;
    
    private Sequence _movingOutsideSeq;
    private Sequence _movingToTrashSeq;
    
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
        _movingOutsideSeq = DOTween.Sequence();
        _movingOutsideSeq.Append(_dish.DOMoveY(_dish.position.y + 0.1f, 0.5f));
        _movingOutsideSeq.Append(_dish.DOMove(EntryPoint.position, duration));
        
        if (_heatingSystem.Dish.DishStatus == DishStatus.Success)
        {
            _movingOutsideSeq.Append(_dish.DOMove(mouthPoint.position, finishedDuration).SetEase(easeToMouth));
        }
        else
        {
            _movingOutsideSeq.Append(_dish.DOMove(trashPoint.position, finishedDuration).SetEase(easeToTrash));
        }
        
        _movingOutsideSeq.SetEase(easeMove).Play();
    }
}
