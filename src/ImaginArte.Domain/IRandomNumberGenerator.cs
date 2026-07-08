namespace ImaginArte.Domain;

public interface IRandomNumberGenerator
{
    int Next(int exclusiveUpperBound);
}

internal sealed class SharedRandomNumberGenerator : IRandomNumberGenerator
{
    public int Next(int exclusiveUpperBound)
    {
        return Random.Shared.Next(exclusiveUpperBound);
    }
}
