using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "GameConfigInstaller", menuName = "Installers/GameConfigInstaller")]
public class GameConfigInstaller : ScriptableObjectInstaller<GameConfigInstaller>
{
    [SerializeField] private GameConfig gameConfig;
    
    public override void InstallBindings()
    {
        Container.BindInstances(gameConfig);
        Container.Bind<TemperatureModel>().AsSingle();
    }
}
