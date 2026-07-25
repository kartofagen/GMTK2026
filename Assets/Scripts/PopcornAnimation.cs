using System;
using R3;
using UnityEngine;


public class PopcornAnimation : MonoBehaviour
{
    [SerializeField] private Mesh[] meshSequence;
    [SerializeField] private float[] tempSequence;

    private MeshFilter _meshFilter;
    private DishComponent _dishComponent;
    private int _curStage = 0;

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _dishComponent = GetComponent<DishComponent>();
        _dishComponent.Temperature.Subscribe(curTemp =>
        {
            if (_curStage + 1 < tempSequence.Length  && curTemp >= tempSequence[_curStage + 1])
            {
                Debug.Log($"Updating mesh, stage:  {_curStage + 1}");
                _meshFilter.sharedMesh =  meshSequence[_curStage + 1];
                _curStage++;
            }
        }).AddTo(this);
    }

    private void Start()
    {
        var cube = GameObject.CreatePrimitive( PrimitiveType.Cube);
        var sphere = GameObject.CreatePrimitive( PrimitiveType.Sphere);

        var mf1 = cube.GetComponent<MeshFilter>();
        var mf2 = sphere.GetComponent<MeshFilter>();

        mf1.mesh = mf2.mesh;
    }
}