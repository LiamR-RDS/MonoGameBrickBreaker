using Microsoft.Xna.Framework;

namespace MonoGameBrickBreaker.Core;

public sealed class LevelSelectScreen
{
    private int selection;

    public int Update(bool actionPressed, bool upPressed, bool downPressed, bool leftPressed, bool rightPressed,
        Point mousePoint)
    {
        for (int i = 0; i < GameConstants.TotalLevels; i++)
        {
            if (GetLevelButton(i).Contains(mousePoint))
                selection = i;
        }

        if (upPressed) selection = System.Math.Max(0, selection - 2);
        if (downPressed) selection = System.Math.Min(GameConstants.TotalLevels - 1, selection + 2);
        if (leftPressed) selection = System.Math.Max(0, selection - 1);
        if (rightPressed) selection = System.Math.Min(GameConstants.TotalLevels - 1, selection + 1);
        if (!actionPressed) return -1;
        return GetBackButton().Contains(mousePoint) ? -2 : selection;
    }

    public void Draw(GameRenderer renderer)
    {
        renderer.DrawTitle("LEVEL SELECT", 55, 2f);
        renderer.DrawCentered("Choose a starting level", 105, Color.LightGray);
        for (int i = 0; i < GameConstants.TotalLevels; i++)
            renderer.DrawButton(GetLevelButton(i), $"Level {i + 1}", i == selection);
        renderer.DrawButton(GetBackButton(), "Back", false);
    }

    private static Rectangle GetLevelButton(int index) =>
        new(180 + index % 2 * 230, 145 + index / 2 * 60, 200, 45);

    private static Rectangle GetBackButton() => new(300, 500, 200, 45);
}