using Microsoft.Xna.Framework;

namespace MonoGameBrickBreaker.Core;

public enum MainMenuAction
{
    None,
    Play,
    LevelSelect,
    Editor,
    Quit
}

public sealed class MainMenuScreen
{
    private int selection;

    public MainMenuAction Update(bool actionPressed, bool upPressed, bool downPressed, Point mousePoint)
    {
        for (int i = 0; i < 4; i++)
        {
            if (GetButton(i).Contains(mousePoint))
                selection = i;
        }

        if (upPressed) selection = (selection + 3) % 4;
        if (downPressed) selection = (selection + 1) % 4;
        if (!actionPressed) return MainMenuAction.None;
        return (MainMenuAction)(selection + 1);
    }

    public void Draw(GameRenderer renderer)
    {
        renderer.DrawTitle("BRICK BREAKER", 70, 2.2f);
        renderer.DrawCentered("Select an option", 125, Color.LightGray);
        for (int i = 0; i < 4; i++)
        {
            string label = i switch
            {
                0 => "Play",
                1 => "Level Select",
                2 => "Level Editor",
                _ => "Quit"
            };
            renderer.DrawButton(GetButton(i), label, i == selection);
        }

        renderer.DrawCentered("Use Up / Down and Enter, or click a button", 500, Color.LightGray);
    }

    private static Rectangle GetButton(int index) => new(250, 170 + index * 65, 300, 50);
}