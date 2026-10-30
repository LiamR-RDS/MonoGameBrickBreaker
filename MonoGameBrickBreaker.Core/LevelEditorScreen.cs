using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGameBrickBreaker.Core;

public sealed class LevelEditorScreen
{
    private readonly LevelDefinition[] levels;
    private readonly LevelFileService fileService;
    private int selection;
    private Point cursor;
    private string status = "Space toggles a brick; Enter saves the level";
    private double statusTime;

    public LevelEditorScreen(LevelDefinition[] levels, LevelFileService fileService)
    {
        this.levels = levels;
        this.fileService = fileService;
    }

    public bool Update(bool actionPressed, bool upPressed, bool downPressed, bool leftPressed, bool rightPressed,
        KeyboardState keys, Point mousePoint, bool mousePressed, bool previousMouseDown, double elapsedSeconds)
    {
        statusTime -= elapsedSeconds;
        for (int i = 0; i < GameConstants.TotalLevels; i++)
        {
            if (GetLevelButton(i).Contains(mousePoint) && mousePressed && !previousMouseDown)
            {
                selection = i;
                LoadLevel();
                return false;
            }
        }

        if (upPressed) cursor.Y = Math.Max(0, cursor.Y - 1);
        if (downPressed) cursor.Y = Math.Min(LevelDefinition.BrickRows - 1, cursor.Y + 1);
        if (leftPressed) cursor.X = Math.Max(0, cursor.X - 1);
        if (rightPressed) cursor.X = Math.Min(LevelDefinition.BrickColumns - 1, cursor.X + 1);
        if (WasPressed(keys, Keys.PageUp))
        {
            selection = Math.Max(0, selection - 1);
            LoadLevel();
        }

        if (WasPressed(keys, Keys.PageDown))
        {
            selection = Math.Min(GameConstants.TotalLevels - 1, selection + 1);
            LoadLevel();
        }

        if (actionPressed)
        {
            if (GetSaveButton().Contains(mousePoint) || WasPressed(keys, Keys.Enter))
                SaveLevel();
            else if (GetBackButton().Contains(mousePoint))
                return true;
            else if (GetGridBounds().Contains(mousePoint))
            {
                cursor = GetCell(mousePoint);
                ToggleCell();
            }
            else if (!WasPressed(keys, Keys.Enter))
                ToggleCell();
        }

        if (statusTime <= 0)
            status = "Space toggles a brick; Enter saves the level";
        return false;
    }

    public void Draw(GameRenderer renderer)
    {
        renderer.DrawTitle("LEVEL EDITOR", 35, 1.8f);
        renderer.DrawString("Level:", new Vector2(625, 100), Color.White, 1.2f);
        for (int i = 0; i < GameConstants.TotalLevels; i++)
            renderer.DrawButton(GetLevelButton(i), $"{i + 1}", i == selection);

        LevelDefinition level = levels[selection];
        for (int row = 0; row < LevelDefinition.BrickRows; row++)
        {
            for (int col = 0; col < LevelDefinition.BrickColumns; col++)
            {
                Rectangle cell = GetCellRectangle(row, col);
                renderer.Fill(cell, level.Bricks[row, col] ? GameConstants.RowColors[row] : new Color(30, 30, 55));
                renderer.DrawOutline(cell, new Color(100, 100, 130));
            }
        }

        renderer.DrawOutline(GetCellRectangle(cursor.Y, cursor.X), Color.White, 3);
        renderer.DrawButton(GetSaveButton(), "Save", false);
        renderer.DrawButton(GetBackButton(), "Back", false);
        renderer.DrawString(status, new Vector2(40, 430), Color.LightGray);
        renderer.DrawString("Arrows move cursor", new Vector2(40, 465), Color.LightGray);
        renderer.DrawString("Space toggles brick", new Vector2(40, 490), Color.LightGray);
        renderer.DrawString("Enter saves", new Vector2(40, 515), Color.LightGray);
    }

    private void LoadLevel()
    {
        cursor = Point.Zero;
        status = $"Editing level {selection + 1}";
        statusTime = 2;
    }

    private void ToggleCell()
    {
        levels[selection].Bricks[cursor.Y, cursor.X] = !levels[selection].Bricks[cursor.Y, cursor.X];
    }

    private void SaveLevel()
    {
        try
        {
            fileService.SaveLevel(selection, levels[selection]);
            status = $"Saved level {selection + 1}";
        }
        catch (IOException)
        {
            status = "Unable to save level";
        }

        statusTime = 3;
    }

    private static bool WasPressed(KeyboardState keys, Keys key) => keys.IsKeyDown(key);

    private static Rectangle GetLevelButton(int index) =>
        new(620 + index % 2 * 65, 120 + index / 2 * 50, 55, 38);

    private static Rectangle GetSaveButton() => new(620, 410, 120, 45);
    private static Rectangle GetBackButton() => new(620, 470, 120, 45);

    private static Rectangle GetGridBounds() => new(GameConstants.EditorGridX, GameConstants.EditorGridY,
        LevelDefinition.BrickColumns * GameConstants.EditorCellWidth,
        LevelDefinition.BrickRows * GameConstants.EditorCellHeight);

    private static Rectangle GetCellRectangle(int row, int col) => new(
        GameConstants.EditorGridX + col * GameConstants.EditorCellWidth,
        GameConstants.EditorGridY + row * GameConstants.EditorCellHeight,
        GameConstants.EditorCellWidth - 4, GameConstants.EditorCellHeight - 4);

    private static Point GetCell(Point point) => new(
        MathHelper.Clamp((point.X - GameConstants.EditorGridX) / GameConstants.EditorCellWidth, 0,
            LevelDefinition.BrickColumns - 1),
        MathHelper.Clamp((point.Y - GameConstants.EditorGridY) / GameConstants.EditorCellHeight, 0,
            LevelDefinition.BrickRows - 1));
}