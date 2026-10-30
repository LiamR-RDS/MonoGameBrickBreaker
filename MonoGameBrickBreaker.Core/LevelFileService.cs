using System;
using System.IO;

namespace MonoGameBrickBreaker.Core;

public sealed class LevelFileService
{
    private readonly string contentRoot;
    private readonly int totalLevels;

    public LevelFileService(string contentRoot, int totalLevels)
    {
        this.contentRoot = contentRoot;
        this.totalLevels = totalLevels;
    }

    public LevelDefinition[] LoadLevels()
    {
        var levels = new LevelDefinition[totalLevels];
        for (int i = 0; i < totalLevels; i++)
        {
            try
            {
                levels[i] = ReadLevel(GetLevelPath(i));
            }
            catch (Exception)
            {
                levels[i] = CreateFallbackLevel(i);
            }
        }

        return levels;
    }

    public void SaveLevel(int levelIndex, LevelDefinition level)
    {
        string path = GetLevelPath(levelIndex);
        string directory = Path.GetDirectoryName(path);
        if (directory != null)
            Directory.CreateDirectory(directory);

        using var writer = new StreamWriter(path, false);
        writer.WriteLine("# MonoGame Brick Breaker level");
        for (int row = 0; row < LevelDefinition.BrickRows; row++)
        {
            for (int col = 0; col < LevelDefinition.BrickColumns; col++)
                writer.Write(level.Bricks[row, col] ? '#' : '.');
            writer.WriteLine();
        }
    }

    private string GetLevelPath(int levelIndex)
    {
        return Path.Combine(AppContext.BaseDirectory, contentRoot, "Levels",
            $"level{levelIndex + 1:00}.level");
    }

    private static LevelDefinition ReadLevel(string path)
    {
        var level = new LevelDefinition();
        int row = 0;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("# MonoGame", StringComparison.Ordinal)
                                 || line.StartsWith("//", StringComparison.Ordinal))
                continue;

            if (row == LevelDefinition.BrickRows)
                break;
            for (int col = 0; col < LevelDefinition.BrickColumns && col < line.Length; col++)
                level.Bricks[row, col] = line[col] == '#';
            row++;
        }

        if (row != LevelDefinition.BrickRows)
            throw new InvalidDataException($"Level file '{path}' must contain {LevelDefinition.BrickRows} rows.");
        return level;
    }

    private static LevelDefinition CreateFallbackLevel(int levelIndex)
    {
        var level = new LevelDefinition();
        for (int row = 0; row < LevelDefinition.BrickRows; row++)
        {
            for (int col = 0; col < LevelDefinition.BrickColumns; col++)
                level.Bricks[row, col] = levelIndex == 0 ||
                                         (row + col + levelIndex) % (levelIndex % 3 + 2) != 0;
        }

        return level;
    }
}