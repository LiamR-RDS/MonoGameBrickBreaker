namespace MonoGameBrickBreaker.Core;

public sealed class LevelDefinition
{
    public const int BrickColumns = 10;
    public const int BrickRows = 6;

    public bool[,] Bricks { get; } = new bool[BrickRows, BrickColumns];
}