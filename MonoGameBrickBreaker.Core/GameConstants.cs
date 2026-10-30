using Microsoft.Xna.Framework;

namespace MonoGameBrickBreaker.Core;

public static class GameConstants
{
    public const int ScreenWidth = 800;
    public const int ScreenHeight = 600;
    public const int TotalLevels = 10;
    public const int BrickGap = 4;
    public const int BrickHeight = 24;
    public const int BrickTop = 60;
    public const int BallSize = 12;
    public const int StartingLives = 3;
    public const float PaddleSpeed = 600f;
    public const float BallBaseSpeed = 360f;
    public const int EditorGridX = 40;
    public const int EditorGridY = 105;
    public const int EditorCellWidth = 52;
    public const int EditorCellHeight = 48;

    public static readonly Color[] RowColors =
    {
        Color.Red, Color.OrangeRed, Color.Orange, Color.Yellow, Color.LimeGreen, Color.DeepSkyBlue
    };
}