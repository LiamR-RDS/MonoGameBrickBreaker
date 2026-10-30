using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGameBrickBreaker.Core;

public enum GameplayState
{
    Ready,
    Playing,
    Won,
    Lost
}

public sealed class GameplayScreen
{
    private readonly LevelDefinition[] levels;
    private readonly List<Rectangle> bricks = new();
    private readonly List<Color> brickColors = new();
    private readonly List<int> brickPoints = new();
    private Rectangle paddle;
    private Vector2 ballPosition;
    private Vector2 ballVelocity;
    private GameplayState state;
    private int currentLevelIndex;
    private int score;
    private int lives;
    private int lastMouseX;

    public GameplayScreen(LevelDefinition[] levels)
    {
        this.levels = levels;
    }

    public int CurrentLevelIndex => currentLevelIndex;
    public GameplayState State => state;

    public void StartLevel(int levelIndex, bool resetScore)
    {
        currentLevelIndex = MathHelper.Clamp(levelIndex, 0, GameConstants.TotalLevels - 1);
        if (resetScore) score = 0;
        lives = GameConstants.StartingLives;
        BuildBricks();
        ResetBall();
    }

    public bool Update(bool actionPressed, KeyboardState keys, MouseState mouse, TouchCollection touches,
        GamePadState gamepad, float dt, int viewportWidth)
    {
        MovePaddle(keys, mouse, touches, gamepad, dt, viewportWidth);
        switch (state)
        {
            case GameplayState.Ready:
                ballPosition = new Vector2(paddle.Center.X - GameConstants.BallSize / 2f,
                    paddle.Top - GameConstants.BallSize);
                if (actionPressed) Launch();
                break;
            case GameplayState.Playing:
                UpdateBall(dt);
                break;
            case GameplayState.Won when actionPressed:
                if (currentLevelIndex < GameConstants.TotalLevels - 1)
                    StartLevel(currentLevelIndex + 1, false);
                else
                    return true;
                break;
            case GameplayState.Lost when actionPressed:
                StartLevel(currentLevelIndex, true);
                break;
        }

        return false;
    }

    public void Draw(GameRenderer renderer)
    {
        for (int i = 0; i < bricks.Count; i++)
            renderer.Fill(bricks[i], brickColors[i]);
        renderer.Fill(paddle, Color.White);
        renderer.Fill(new Rectangle((int)ballPosition.X, (int)ballPosition.Y,
            GameConstants.BallSize, GameConstants.BallSize), Color.Cyan);
        renderer.DrawString($"Level {currentLevelIndex + 1}/{GameConstants.TotalLevels}", new Vector2(10, 10),
            Color.White);
        renderer.DrawString($"Score: {score}", new Vector2(160, 10), Color.White);
        renderer.DrawString($"Lives: {lives}", new Vector2(GameConstants.ScreenWidth - 110, 10), Color.White);

        string message = state switch
        {
            GameplayState.Ready => "Press Space / click / tap to launch",
            GameplayState.Won when currentLevelIndex < GameConstants.TotalLevels - 1 =>
                "Level complete! Press Space for the next level",
            GameplayState.Won => "All levels complete! Press Space for the main menu",
            GameplayState.Lost => "Game over! Press Space to restart the level",
            _ => null
        };
        if (message != null)
            renderer.DrawCentered(message, GameConstants.ScreenHeight / 2f, Color.White);
    }

    private void BuildBricks()
    {
        bricks.Clear();
        brickColors.Clear();
        brickPoints.Clear();
        LevelDefinition level = levels[currentLevelIndex];
        int width = (GameConstants.ScreenWidth - (LevelDefinition.BrickColumns + 1) * GameConstants.BrickGap) /
                    LevelDefinition.BrickColumns;
        for (int row = 0; row < LevelDefinition.BrickRows; row++)
        {
            for (int col = 0; col < LevelDefinition.BrickColumns; col++)
            {
                if (!level.Bricks[row, col]) continue;
                bricks.Add(new Rectangle(GameConstants.BrickGap + col * (width + GameConstants.BrickGap),
                    GameConstants.BrickTop + row * (GameConstants.BrickHeight + GameConstants.BrickGap),
                    width, GameConstants.BrickHeight));
                brickColors.Add(GameConstants.RowColors[row % GameConstants.RowColors.Length]);
                brickPoints.Add((LevelDefinition.BrickRows - row) * 10);
            }
        }
    }

    private void ResetBall()
    {
        paddle = new Rectangle((GameConstants.ScreenWidth - 100) / 2, GameConstants.ScreenHeight - 40, 100, 14);
        ballPosition = new Vector2(paddle.Center.X - GameConstants.BallSize / 2f,
            paddle.Top - GameConstants.BallSize);
        ballVelocity = Vector2.Zero;
        state = GameplayState.Ready;
    }

    private void Launch()
    {
        float angle = MathHelper.ToRadians(-90 + Random.Shared.Next(-30, 31));
        ballVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * GameConstants.BallBaseSpeed;
        state = GameplayState.Playing;
    }

    private void MovePaddle(KeyboardState keys, MouseState mouse, TouchCollection touches, GamePadState gamepad,
        float dt, int viewportWidth)
    {
        float move = 0;
        if (keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.A)) move -= GameConstants.PaddleSpeed * dt;
        if (keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D)) move += GameConstants.PaddleSpeed * dt;
        move += gamepad.ThumbSticks.Left.X * GameConstants.PaddleSpeed * dt;
        int x = paddle.X + (int)move;
        if (touches.Count > 0)
            x = (int)(touches[0].Position.X * GameConstants.ScreenWidth / viewportWidth) - paddle.Width / 2;
        else if (mouse.X != lastMouseX)
            x = (int)(mouse.X * (float)GameConstants.ScreenWidth / viewportWidth) - paddle.Width / 2;
        lastMouseX = mouse.X;
        paddle.X = MathHelper.Clamp(x, 0, GameConstants.ScreenWidth - paddle.Width);
    }

    private void UpdateBall(float dt)
    {
        ballPosition += ballVelocity * dt;
        if (ballPosition.X < 0)
        {
            ballPosition.X = 0;
            ballVelocity.X = Math.Abs(ballVelocity.X);
        }

        if (ballPosition.X + GameConstants.BallSize > GameConstants.ScreenWidth)
        {
            ballPosition.X = GameConstants.ScreenWidth - GameConstants.BallSize;
            ballVelocity.X = -Math.Abs(ballVelocity.X);
        }

        if (ballPosition.Y < 0)
        {
            ballPosition.Y = 0;
            ballVelocity.Y = Math.Abs(ballVelocity.Y);
        }

        var ball = new Rectangle((int)ballPosition.X, (int)ballPosition.Y,
            GameConstants.BallSize, GameConstants.BallSize);
        if (ballVelocity.Y > 0 && ball.Intersects(paddle))
        {
            float offset = MathHelper.Clamp((ball.Center.X - paddle.Center.X) / (paddle.Width / 2f), -0.9f, 0.9f);
            float speed = ballVelocity.Length();
            ballVelocity = new Vector2(offset, -(float)Math.Sqrt(1 - offset * offset)) * speed;
            ballPosition.Y = paddle.Top - GameConstants.BallSize;
        }

        for (int i = 0; i < bricks.Count; i++)
        {
            if (!ball.Intersects(bricks[i])) continue;
            Rectangle overlap = Rectangle.Intersect(ball, bricks[i]);
            if (overlap.Width < overlap.Height) ballVelocity.X = -ballVelocity.X;
            else ballVelocity.Y = -ballVelocity.Y;
            score += brickPoints[i];
            bricks.RemoveAt(i);
            brickColors.RemoveAt(i);
            brickPoints.RemoveAt(i);
            ballVelocity *= 1.01f;
            break;
        }

        if (bricks.Count == 0) state = GameplayState.Won;
        else if (ballPosition.Y > GameConstants.ScreenHeight)
        {
            lives--;
            if (lives <= 0) state = GameplayState.Lost;
            else ResetBall();
        }
    }
}