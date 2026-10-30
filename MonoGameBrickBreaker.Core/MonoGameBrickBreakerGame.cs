using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGameBrickBreaker.Core
{
    /// <summary>
    /// A simple brick breaker game with a main menu, level select, and level editor.
    /// </summary>
    public class MonoGameBrickBreakerGame : Game
    {
        private enum Screen
        {
            MainMenu,
            LevelSelect,
            Editor,
            Game
        }

        private enum State
        {
            Ready,
            Playing,
            Won,
            Lost
        }

        private sealed class LevelDefinition
        {
            public bool[,] Bricks { get; } = new bool[BrickRows, BrickColumns];
        }

        private const int ScreenWidth = 800;
        private const int ScreenHeight = 600;
        private const int BrickColumns = 10;
        private const int BrickRows = 6;
        private const int TotalLevels = 10;
        private const int BrickGap = 4;
        private const int BrickHeight = 24;
        private const int BrickTop = 60;
        private const int BallSize = 12;
        private const int StartingLives = 3;
        private const float PaddleSpeed = 600f;
        private const float BallBaseSpeed = 360f;
        private const int EditorGridX = 40;
        private const int EditorGridY = 105;
        private const int EditorCellWidth = 52;
        private const int EditorCellHeight = 48;

        private SpriteBatch spriteBatch;
        private Texture2D pixel;
        private SpriteFont font;

        private readonly List<Rectangle> bricks = new();
        private readonly List<Color> brickColors = new();
        private readonly List<int> brickPoints = new();
        private readonly LevelDefinition[] levels = new LevelDefinition[TotalLevels];
        private Rectangle paddle;
        private Vector2 ballPosition;
        private Vector2 ballVelocity;
        private State state;
        private Screen screen = Screen.MainMenu;
        private int score;
        private int lives;
        private int currentLevelIndex;
        private int menuSelection;
        private int levelSelection;
        private int editorSelection;
        private Point editorCursor;
        private string editorStatus;
        private double editorStatusTime;
        private int lastMouseX;
        private bool prevActionDown;
        private KeyboardState previousKeyboardState;
        private bool previousMouseDown;
        private bool previousGamepadBackDown;

        private static readonly Color[] RowColors =
        {
            Color.Red, Color.OrangeRed, Color.Orange, Color.Yellow, Color.LimeGreen, Color.DeepSkyBlue
        };

        public MonoGameBrickBreakerGame()
        {
            _ = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = ScreenWidth,
                PreferredBackBufferHeight = ScreenHeight,
                SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.Title = "Brick Breaker";
            TouchPanel.EnabledGestures = GestureType.None;
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
            font = Content.Load<SpriteFont>("Fonts/Hud");
            LoadLevels();
            previousKeyboardState = Keyboard.GetState();
            ResetToMainMenu();
        }

        private void LoadLevels()
        {
            for (int i = 0; i < TotalLevels; i++)
            {
                string path = GetLevelPath(i);
                try
                {
                    levels[i] = ReadLevel(path);
                }
                catch (Exception)
                {
                    levels[i] = CreateFallbackLevel(i);
                }
            }
        }

        private string GetLevelPath(int levelIndex)
        {
            return Path.Combine(AppContext.BaseDirectory, Content.RootDirectory, "Levels",
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

                for (int col = 0; col < BrickColumns && col < line.Length; col++)
                    level.Bricks[row, col] = line[col] == '#';
                row++;
                if (row == BrickRows)
                    break;
            }

            if (row != BrickRows)
                throw new InvalidDataException($"Level file '{path}' must contain {BrickRows} rows.");
            return level;
        }

        private static LevelDefinition CreateFallbackLevel(int levelIndex)
        {
            var level = new LevelDefinition();
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickColumns; col++)
                {
                    level.Bricks[row, col] = levelIndex == 0 || (row + col + levelIndex) % (levelIndex % 3 + 2) != 0;
                }
            }

            return level;
        }

        private void ResetToMainMenu()
        {
            screen = Screen.MainMenu;
            menuSelection = 0;
            levelSelection = 0;
            editorSelection = 0;
        }

        private void StartLevel(int levelIndex, bool resetScore)
        {
            currentLevelIndex = MathHelper.Clamp(levelIndex, 0, TotalLevels - 1);
            if (resetScore)
                score = 0;
            lives = StartingLives;
            screen = Screen.Game;
            BuildBricks();
            ResetBall();
        }

        private void BuildBricks()
        {
            bricks.Clear();
            brickColors.Clear();
            brickPoints.Clear();
            LevelDefinition level = levels[currentLevelIndex];
            int width = (ScreenWidth - (BrickColumns + 1) * BrickGap) / BrickColumns;
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickColumns; col++)
                {
                    if (!level.Bricks[row, col])
                        continue;
                    bricks.Add(new Rectangle(
                        BrickGap + col * (width + BrickGap),
                        BrickTop + row * (BrickHeight + BrickGap),
                        width, BrickHeight));
                    brickColors.Add(RowColors[row % RowColors.Length]);
                    brickPoints.Add((BrickRows - row) * 10);
                }
            }
        }

        private void ResetBall()
        {
            paddle = new Rectangle((ScreenWidth - 100) / 2, ScreenHeight - 40, 100, 14);
            ballPosition = new Vector2(paddle.Center.X - BallSize / 2f, paddle.Top - BallSize);
            ballVelocity = Vector2.Zero;
            state = State.Ready;
        }

        private void Launch()
        {
            float angle = MathHelper.ToRadians(-90 + Random.Shared.Next(-30, 31));
            ballVelocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * BallBaseSpeed;
            state = State.Playing;
        }

        protected override void Update(GameTime gameTime)
        {
            var keys = Keyboard.GetState();
            var mouse = Mouse.GetState();
            var touches = TouchPanel.GetState();
            var gamepad = GamePad.GetState(PlayerIndex.One);
            bool actionDown = keys.IsKeyDown(Keys.Space) || keys.IsKeyDown(Keys.Enter)
                                                         || mouse.LeftButton == ButtonState.Pressed || touches.Count > 0
                                                         || gamepad.Buttons.A == ButtonState.Pressed;
            bool actionPressed = actionDown && !prevActionDown;
            bool upPressed = WasKeyPressed(keys, Keys.Up) || WasKeyPressed(keys, Keys.W);
            bool downPressed = WasKeyPressed(keys, Keys.Down) || WasKeyPressed(keys, Keys.S);
            bool leftPressed = WasKeyPressed(keys, Keys.Left) || WasKeyPressed(keys, Keys.A);
            bool rightPressed = WasKeyPressed(keys, Keys.Right) || WasKeyPressed(keys, Keys.D);
            bool backPressed = WasKeyPressed(keys, Keys.Escape)
                               || gamepad.Buttons.Back == ButtonState.Pressed && !previousGamepadBackDown;

            if (backPressed)
            {
                if (screen == Screen.Game || screen == Screen.MainMenu)
                    Exit();
                else
                    screen = Screen.MainMenu;
            }

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            switch (screen)
            {
                case Screen.MainMenu:
                    UpdateMainMenu(actionPressed, upPressed, downPressed, mouse);
                    break;
                case Screen.LevelSelect:
                    UpdateLevelSelect(actionPressed, upPressed, downPressed, leftPressed, rightPressed, mouse);
                    break;
                case Screen.Editor:
                    UpdateEditor(actionPressed, upPressed, downPressed, leftPressed, rightPressed, keys, mouse);
                    break;
                case Screen.Game:
                    UpdateGame(actionPressed, keys, mouse, touches, dt);
                    break;
            }

            if (editorStatusTime > 0)
                editorStatusTime -= gameTime.ElapsedGameTime.TotalSeconds;
            prevActionDown = actionDown;
            previousKeyboardState = keys;
            previousMouseDown = mouse.LeftButton == ButtonState.Pressed;
            previousGamepadBackDown = gamepad.Buttons.Back == ButtonState.Pressed;
            base.Update(gameTime);
        }

        private void UpdateMainMenu(bool actionPressed, bool upPressed, bool downPressed, MouseState mouse)
        {
            Point mousePoint = GetVirtualMousePosition(mouse);
            for (int i = 0; i < 4; i++)
            {
                if (GetMainMenuButton(i).Contains(mousePoint))
                    menuSelection = i;
            }

            if (upPressed)
                menuSelection = (menuSelection + 3) % 4;
            if (downPressed)
                menuSelection = (menuSelection + 1) % 4;
            if (actionPressed)
            {
                switch (menuSelection)
                {
                    case 0:
                        StartLevel(0, true);
                        break;
                    case 1:
                        screen = Screen.LevelSelect;
                        break;
                    case 2:
                        screen = Screen.Editor;
                        LoadEditorLevel();
                        break;
                    case 3:
                        Exit();
                        break;
                }
            }
        }

        private void UpdateLevelSelect(bool actionPressed, bool upPressed, bool downPressed,
            bool leftPressed, bool rightPressed, MouseState mouse)
        {
            Point mousePoint = GetVirtualMousePosition(mouse);
            for (int i = 0; i < TotalLevels; i++)
            {
                if (GetLevelButton(i).Contains(mousePoint))
                    levelSelection = i;
            }

            if (upPressed)
                levelSelection = Math.Max(0, levelSelection - 2);
            if (downPressed)
                levelSelection = Math.Min(TotalLevels - 1, levelSelection + 2);
            if (leftPressed)
                levelSelection = Math.Max(0, levelSelection - 1);
            if (rightPressed)
                levelSelection = Math.Min(TotalLevels - 1, levelSelection + 1);

            if (actionPressed)
            {
                if (GetLevelSelectBackButton().Contains(mousePoint))
                    screen = Screen.MainMenu;
                else
                    StartLevel(levelSelection, true);
            }
        }

        private void UpdateEditor(bool actionPressed, bool upPressed, bool downPressed,
            bool leftPressed, bool rightPressed, KeyboardState keys, MouseState mouse)
        {
            Point mousePoint = GetVirtualMousePosition(mouse);
            for (int i = 0; i < TotalLevels; i++)
            {
                if (GetEditorLevelButton(i).Contains(mousePoint)
                    && mouse.LeftButton == ButtonState.Pressed && !previousMouseDown)
                {
                    editorSelection = i;
                    LoadEditorLevel();
                    return;
                }
            }

            if (upPressed) editorCursor.Y = Math.Max(0, editorCursor.Y - 1);
            if (downPressed) editorCursor.Y = Math.Min(BrickRows - 1, editorCursor.Y + 1);
            if (leftPressed) editorCursor.X = Math.Max(0, editorCursor.X - 1);
            if (rightPressed) editorCursor.X = Math.Min(BrickColumns - 1, editorCursor.X + 1);

            if (WasKeyPressed(keys, Keys.PageUp))
            {
                editorSelection = Math.Max(0, editorSelection - 1);
                LoadEditorLevel();
            }

            if (WasKeyPressed(keys, Keys.PageDown))
            {
                editorSelection = Math.Min(TotalLevels - 1, editorSelection + 1);
                LoadEditorLevel();
            }

            if (actionPressed)
            {
                if (GetEditorSaveButton().Contains(mousePoint) || WasKeyPressed(keys, Keys.Enter))
                {
                    SaveEditorLevel();
                }
                else if (GetEditorBackButton().Contains(mousePoint))
                {
                    screen = Screen.MainMenu;
                }
                else if (GetEditorGridBounds().Contains(mousePoint))
                {
                    editorCursor = GetEditorCell(mousePoint);
                    ToggleEditorCell();
                }
                else if (!WasKeyPressed(keys, Keys.Enter))
                {
                    ToggleEditorCell();
                }
            }

            if (editorStatusTime <= 0)
                editorStatus = "Space toggles a brick; Enter saves the level";
        }

        private void LoadEditorLevel()
        {
            editorCursor = Point.Zero;
            editorStatus = $"Editing level {editorSelection + 1}";
            editorStatusTime = 2;
        }

        private void ToggleEditorCell()
        {
            LevelDefinition level = levels[editorSelection];
            level.Bricks[editorCursor.Y, editorCursor.X] = !level.Bricks[editorCursor.Y, editorCursor.X];
        }

        private void SaveEditorLevel()
        {
            try
            {
                string path = GetLevelPath(editorSelection);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using var writer = new StreamWriter(path, false);
                writer.WriteLine("# MonoGame Brick Breaker level");
                for (int row = 0; row < BrickRows; row++)
                {
                    for (int col = 0; col < BrickColumns; col++)
                        writer.Write(levels[editorSelection].Bricks[row, col] ? '#' : '.');
                    writer.WriteLine();
                }

                editorStatus = $"Saved level {editorSelection + 1}";
                editorStatusTime = 3;
            }
            catch (IOException)
            {
                editorStatus = "Unable to save level";
                editorStatusTime = 3;
            }
        }

        private void UpdateGame(bool actionPressed, KeyboardState keys, MouseState mouse,
            TouchCollection touches, float dt)
        {
            MovePaddle(keys, mouse, touches, dt);
            switch (state)
            {
                case State.Ready:
                    ballPosition = new Vector2(paddle.Center.X - BallSize / 2f, paddle.Top - BallSize);
                    if (actionPressed) Launch();
                    break;
                case State.Playing:
                    UpdateBall(dt);
                    break;
                case State.Won:
                    if (actionPressed)
                    {
                        if (currentLevelIndex < TotalLevels - 1)
                            StartLevel(currentLevelIndex + 1, false);
                        else
                            ResetToMainMenu();
                    }

                    break;
                case State.Lost:
                    if (actionPressed)
                        StartLevel(currentLevelIndex, true);
                    break;
            }
        }

        private void MovePaddle(KeyboardState keys, MouseState mouse, TouchCollection touches, float dt)
        {
            float move = 0;
            if (keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.A)) move -= PaddleSpeed * dt;
            if (keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D)) move += PaddleSpeed * dt;
            move += GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.X * PaddleSpeed * dt;

            int x = paddle.X + (int)move;
            if (touches.Count > 0)
                x = (int)(touches[0].Position.X * ScreenWidth / GraphicsDevice.Viewport.Width) - paddle.Width / 2;
            else if (mouse.X != lastMouseX)
                x = (int)(mouse.X * (float)ScreenWidth / GraphicsDevice.Viewport.Width) - paddle.Width / 2;
            lastMouseX = mouse.X;
            paddle.X = MathHelper.Clamp(x, 0, ScreenWidth - paddle.Width);
        }

        private void UpdateBall(float dt)
        {
            ballPosition += ballVelocity * dt;

            if (ballPosition.X < 0)
            {
                ballPosition.X = 0;
                ballVelocity.X = Math.Abs(ballVelocity.X);
            }

            if (ballPosition.X + BallSize > ScreenWidth)
            {
                ballPosition.X = ScreenWidth - BallSize;
                ballVelocity.X = -Math.Abs(ballVelocity.X);
            }

            if (ballPosition.Y < 0)
            {
                ballPosition.Y = 0;
                ballVelocity.Y = Math.Abs(ballVelocity.Y);
            }

            var ball = new Rectangle((int)ballPosition.X, (int)ballPosition.Y, BallSize, BallSize);
            if (ballVelocity.Y > 0 && ball.Intersects(paddle))
            {
                float offset = (ball.Center.X - paddle.Center.X) / (paddle.Width / 2f);
                offset = MathHelper.Clamp(offset, -0.9f, 0.9f);
                float speed = ballVelocity.Length();
                ballVelocity = new Vector2(offset, -(float)Math.Sqrt(1 - offset * offset)) * speed;
                ballPosition.Y = paddle.Top - BallSize;
            }

            for (int i = 0; i < bricks.Count; i++)
            {
                if (!ball.Intersects(bricks[i])) continue;
                var overlap = Rectangle.Intersect(ball, bricks[i]);
                if (overlap.Width < overlap.Height) ballVelocity.X = -ballVelocity.X;
                else ballVelocity.Y = -ballVelocity.Y;
                score += brickPoints[i];
                bricks.RemoveAt(i);
                brickColors.RemoveAt(i);
                brickPoints.RemoveAt(i);
                ballVelocity *= 1.01f;
                break;
            }

            if (bricks.Count == 0)
            {
                state = State.Won;
            }
            else if (ballPosition.Y > ScreenHeight)
            {
                lives--;
                if (lives <= 0) state = State.Lost;
                else ResetBall();
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(15, 15, 35));
            spriteBatch.Begin(samplerState: SamplerState.PointClamp,
                transformMatrix: Matrix.CreateScale(
                    GraphicsDevice.Viewport.Width / (float)ScreenWidth,
                    GraphicsDevice.Viewport.Height / (float)ScreenHeight, 1f));

            switch (screen)
            {
                case Screen.MainMenu:
                    DrawMainMenu();
                    break;
                case Screen.LevelSelect:
                    DrawLevelSelect();
                    break;
                case Screen.Editor:
                    DrawEditor();
                    break;
                case Screen.Game:
                    DrawGame();
                    break;
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawMainMenu()
        {
            DrawTitle("BRICK BREAKER", 70, 2.2f);
            DrawCentered("Select an option", 125, Color.LightGray);
            for (int i = 0; i < 4; i++)
            {
                string label = i switch
                {
                    0 => "Play",
                    1 => "Level Select",
                    2 => "Level Editor",
                    _ => "Quit"
                };
                DrawButton(GetMainMenuButton(i), label, i == menuSelection);
            }

            DrawCentered("Use Up / Down and Enter, or click a button", 500, Color.LightGray);
        }

        private void DrawLevelSelect()
        {
            DrawTitle("LEVEL SELECT", 55, 2f);
            DrawCentered("Choose a starting level", 105, Color.LightGray);
            for (int i = 0; i < TotalLevels; i++)
                DrawButton(GetLevelButton(i), $"Level {i + 1}", i == levelSelection);
            DrawButton(GetLevelSelectBackButton(), "Back", false);
        }

        private void DrawEditor()
        {
            DrawTitle("LEVEL EDITOR", 35, 1.8f);
            DrawString("Level:", new Vector2(625, 100), Color.White, 1.2f);
            for (int i = 0; i < TotalLevels; i++)
                DrawButton(GetEditorLevelButton(i), $"{i + 1}", i == editorSelection);

            LevelDefinition level = levels[editorSelection];
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickColumns; col++)
                {
                    Rectangle cell = GetEditorCellRectangle(row, col);
                    Fill(cell, level.Bricks[row, col] ? RowColors[row] : new Color(30, 30, 55));
                    DrawOutline(cell, new Color(100, 100, 130));
                }
            }

            DrawOutline(GetEditorCellRectangle(editorCursor.Y, editorCursor.X), Color.White, 3);
            DrawButton(GetEditorSaveButton(), "Save", false);
            DrawButton(GetEditorBackButton(), "Back", false);
            DrawString(editorStatus, new Vector2(40, 430), Color.LightGray, 1f);
            DrawString("Arrows move cursor", new Vector2(40, 465), Color.LightGray, 1f);
            DrawString("Space toggles brick", new Vector2(40, 490), Color.LightGray, 1f);
            DrawString("Enter saves", new Vector2(40, 515), Color.LightGray, 1f);
        }

        private void DrawGame()
        {
            for (int i = 0; i < bricks.Count; i++)
                Fill(bricks[i], brickColors[i]);
            Fill(paddle, Color.White);
            Fill(new Rectangle((int)ballPosition.X, (int)ballPosition.Y, BallSize, BallSize), Color.Cyan);

            DrawString($"Level {currentLevelIndex + 1}/{TotalLevels}", new Vector2(10, 10), Color.White, 1f);
            DrawString($"Score: {score}", new Vector2(160, 10), Color.White, 1f);
            DrawString($"Lives: {lives}", new Vector2(ScreenWidth - 110, 10), Color.White, 1f);

            string message = state switch
            {
                State.Ready => "Press Space / click / tap to launch",
                State.Won when currentLevelIndex < TotalLevels - 1 => "Level complete! Press Space for the next level",
                State.Won => "All levels complete! Press Space for the main menu",
                State.Lost => "Game over! Press Space to restart the level",
                _ => null
            };
            if (message != null)
                DrawCentered(message, ScreenHeight / 2f, Color.White);
        }

        private void DrawTitle(string text, float y, float scale)
        {
            Vector2 size = font.MeasureString(text) * scale;
            DrawString(text, new Vector2((ScreenWidth - size.X) / 2f, y), Color.Cyan, scale);
        }

        private void DrawCentered(string text, float y, Color color)
        {
            Vector2 size = font.MeasureString(text);
            DrawString(text, new Vector2((ScreenWidth - size.X) / 2f, y), color, 1f);
        }

        private void DrawButton(Rectangle rectangle, string text, bool selected)
        {
            Fill(rectangle, selected ? new Color(35, 105, 150) : new Color(35, 35, 65));
            DrawOutline(rectangle, selected ? Color.Cyan : new Color(100, 100, 130), selected ? 2 : 1);
            Vector2 size = font.MeasureString(text);
            DrawString(text, new Vector2(rectangle.Center.X - size.X / 2f, rectangle.Center.Y - size.Y / 2f),
                Color.White, 1f);
        }

        private void DrawString(string text, Vector2 position, Color color, float scale)
        {
            spriteBatch.DrawString(font, text, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
        }

        private void DrawOutline(Rectangle rectangle, Color color, int thickness = 1)
        {
            Fill(new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
            Fill(new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            Fill(new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
            Fill(new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
        }

        private Rectangle GetMainMenuButton(int index) => new(250, 170 + index * 65, 300, 50);

        private Rectangle GetLevelButton(int index)
        {
            int column = index % 2;
            int row = index / 2;
            return new Rectangle(180 + column * 230, 145 + row * 60, 200, 45);
        }

        private Rectangle GetLevelSelectBackButton() => new(300, 500, 200, 45);

        private Rectangle GetEditorLevelButton(int index)
        {
            int column = index % 2;
            int row = index / 2;
            return new Rectangle(620 + column * 65, 120 + row * 50, 55, 38);
        }

        private Rectangle GetEditorSaveButton() => new(620, 410, 120, 45);

        private Rectangle GetEditorBackButton() => new(620, 470, 120, 45);

        private Rectangle GetEditorGridBounds() => new(EditorGridX, EditorGridY, BrickColumns * EditorCellWidth,
            BrickRows * EditorCellHeight);

        private Rectangle GetEditorCellRectangle(int row, int col)
        {
            return new Rectangle(EditorGridX + col * EditorCellWidth, EditorGridY + row * EditorCellHeight,
                EditorCellWidth - 4, EditorCellHeight - 4);
        }

        private Point GetEditorCell(Point point)
        {
            return new Point(
                MathHelper.Clamp((point.X - EditorGridX) / EditorCellWidth, 0, BrickColumns - 1),
                MathHelper.Clamp((point.Y - EditorGridY) / EditorCellHeight, 0, BrickRows - 1));
        }

        private Point GetVirtualMousePosition(MouseState mouse)
        {
            return new Point(
                (int)(mouse.X * (float)ScreenWidth / GraphicsDevice.Viewport.Width),
                (int)(mouse.Y * (float)ScreenHeight / GraphicsDevice.Viewport.Height));
        }

        private bool WasKeyPressed(KeyboardState keys, Keys key)
        {
            return keys.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }

        private void Fill(Rectangle rectangle, Color color) => spriteBatch.Draw(pixel, rectangle, color);
    }
}