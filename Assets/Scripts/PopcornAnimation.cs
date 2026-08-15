using System;
using R3;
using UnityEngine;


public class PopcornAnimation : MonoBehaviour
{
    [SerializeField] private Mesh[] meshSequence;
    // [SerializeField] private float[] tempSequence;

    private MeshFilter _meshFilter;
    private DishComponent _dishComponent;
    private int _curStage = 0;

    void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _dishComponent = GetComponent<DishComponent>();

        var c = _dishComponent.Config;
        
        _dishComponent.Temperature.Subscribe(curTemp =>
        {
            switch (_curStage)
            {
                case 0:
                    if (curTemp >= (c.t0 + c.tOptLow) * 1 / 4) // (c.t0 + c.tOptLow) / 2
                    {
                        UpdateMesh();
                    }
                    break;
                case 1:
                    if (curTemp >= (c.t0 + c.tOptLow) * 2 / 4) // c.tOptLow
                    {
                        UpdateMesh();
                    }
                    break;
                case 2:
                    if (curTemp >= (c.t0 + c.tOptLow) * 3 / 4) // (c.tOptLow + c.tOptHigh) / 2
                    {
                        UpdateMesh();
                    }
                    break;
            }
        }).AddTo(this);
    }

    private void UpdateMesh()
    {
        _curStage++;
        _meshFilter.mesh = meshSequence[_curStage];
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