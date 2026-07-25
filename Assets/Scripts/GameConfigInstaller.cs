using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "GameConfigInstaller", menuName = "Installers/GameConfigInstaller")]
public class GameConfigInstaller : ScriptableObjectInstaller<GameConfigInstaller>
{
    public override void InstallBindings()
    {
        // Параметры нагрева живут в LevelConfig конкретного блюда, а не глобально:
        // контекст раздаёт активный уровень тем, кому он нужен (график, кнопка нагрева).
        Container.Bind<MicrowaveContext>().AsSingle();
    }
}
