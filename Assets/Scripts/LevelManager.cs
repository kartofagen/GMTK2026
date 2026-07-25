using UnityEngine;
using Zenject;

[System.Serializable]
public class LevelDishes
{
    public GameObject[] dishes;
}

public class LevelManager : MonoBehaviour
{
    [SerializeField] private LevelDishes[] dishesSequence;
    [SerializeField] private int levelIndex;
    [SerializeField] private float spawnRadius;
    
    [Inject] private IInstantiator _instantiator;

    private GameObject[] _currentSpawned;
    
    void Start()
    {
        SpawnDishes();
    }
    
    void SpawnDishes()
    {
        GameObject[] currentDishes = dishesSequence[levelIndex].dishes;

        int count = currentDishes.Length;
        
        _currentSpawned = new GameObject[count];

        for (int i = 0; i < count; ++i)
        {
            _currentSpawned[i] = _instantiator.InstantiatePrefab(currentDishes[i], transform);
            _currentSpawned[i].GetComponent<Dish>().Index = i;
        }

        PlaceDishes();
    }

    public void ManageLevel(Dish dish)
    {
        PlaceDishes();
        
        if (dish.DishStatus == DishStatus.Success)
        {
            DestroyDish(dish);
        }
        else
        {
            dish.Reset();
        }
        
        TryNextLevel();
    }

    public void PlaceDishes()
    {
        GameObject[] currentDishes = dishesSequence[levelIndex].dishes;
        int count = currentDishes.Length;

        for (int i = 0; i < count; ++i)
        {
            if (!_currentSpawned[i]) continue;
            
            float angle = (i * 360f) / count;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 spawnPosition = transform.position + direction * spawnRadius;

            _currentSpawned[i].transform.position = spawnPosition;
            _currentSpawned[i].transform.rotation = Quaternion.identity;
        }
    }

    private void DestroyDish(Dish dish)
    {
        Destroy(_currentSpawned[dish.Index]);
        _currentSpawned[dish.Index] = null;
    }

    private void TryNextLevel()
    {
        GameObject[] currentDishes = dishesSequence[levelIndex].dishes;
        int count = currentDishes.Length;

        for (int i = 0; i < count; ++i)
        {
            if (_currentSpawned[i]) return;
        }
        Debug.Log("NEXT LEVEL");

        if (levelIndex + 1 < dishesSequence.Length)
        {
            ++levelIndex;
            SpawnDishes();
        }
    }
}
