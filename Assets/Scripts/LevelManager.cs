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
    [SerializeField] private int index;
    [SerializeField] private float spawnRadius;
    
    [Inject] private IInstantiator _instantiator; 
    
    void Start()
    {
        SpawnDishes();
    }
    
    void SpawnDishes()
    {
        GameObject[] currentDishes = dishesSequence[index].dishes;

        int count = currentDishes.Length;

        for (int i = 0; i < count; ++i)
        {
            float angle = (i * 360f) / count;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 direction = rotation * Vector3.forward;
            Vector3 spawnPosition = transform.position + direction * spawnRadius;

            _instantiator.InstantiatePrefab(currentDishes[i], spawnPosition, Quaternion.identity, transform);
        }
    }
}
