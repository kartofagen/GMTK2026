using R3;

public class TemperatureModel
{
    public ReactiveProperty<float> Temperature { get; } = new(20f);
}
