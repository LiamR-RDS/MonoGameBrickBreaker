using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGameBrickBreaker.Core
{
    /// <summary>
    /// A simple brick breaker game drawn entirely with geometric shapes.
    /// Controls: Left/Right or A/D (or mouse/touch) to move the paddle,
    /// Space/Enter/click/tap to launch the ball or restart, Escape to exit.
    /// </summary>
    public class MonoGameBrickBreakerGame : Game
    {
        private enum State { Ready, Playing, Won, Lost }

        private const int ScreenWidth = 800;
        private const int ScreenHeight = 600;
        private const int BrickColumns = 10;
        private const int BrickRows = 6;
        private const int BrickGap = 4;
        private const int BrickHeight = 24;
        private const int BrickTop = 60;
        private const int BallSize = 12;
        private const int StartingLives = 3;
        private const float PaddleSpeed = 600f;
        private const float BallBaseSpeed = 360f;

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Texture2D pixel;
        private SpriteFont font;

        private readonly List<Rectangle> bricks = new();
        private readonly List<Color> brickColors = new();
        private readonly List<int> brickPoints = new();
        private Rectangle paddle;
        private Vector2 ballPosition;
        private Vector2 ballVelocity;
        private State state;
        private int score;
        private int lives;
        private bool prevActionDown = true;

        private static readonly Color[] RowColors =
        {
            Color.Red, Color.OrangeRed, Color.Orange, Color.Yellow, Color.LimeGreen, Color.DeepSkyBlue
        };

        public readonly static bool IsMobile = OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

        public MonoGameBrickBreakerGame()
        {
            graphics = new GraphicsDeviceManager(this)
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
            // Placeholder: load sound effects here, e.g.
            // paddleHitSound = Content.Load<SoundEffect>("Sounds/PaddleHit");
            NewGame();
        }

        private void NewGame()
        {
            score = 0;
            lives = StartingLives;
            BuildBricks();
            ResetBall();
        }

        private void BuildBricks()
        {
            bricks.Clear();
            brickColors.Clear();
            brickPoints.Clear();
            int width = (ScreenWidth - (BrickColumns + 1) * BrickGap) / BrickColumns;
            for (int row = 0; row < BrickRows; row++)
            {
                for (int col = 0; col < BrickColumns; col++)
                {
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
            PlaySound("Launch");
        }

        // Placeholder for sound effects: replace the body with real SoundEffect playback.
        private static void PlaySound(string name)
        {
            // TODO: play sound effect identified by 'name' (Launch, PaddleHit, WallHit, BrickBreak, LoseLife, Win, Lose).
        }

        protected override void Update(GameTime gameTime)
        {
            var keys = Keyboard.GetState();
            var mouse = Mouse.GetState();
            var touches = TouchPanel.GetState();
            if (keys.IsKeyDown(Keys.Escape) || GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            MovePaddle(keys, mouse, touches, dt);

            bool actionDown = keys.IsKeyDown(Keys.Space) || keys.IsKeyDown(Keys.Enter)
                || mouse.LeftButton == ButtonState.Pressed || touches.Count > 0
                || GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed;
            bool actionPressed = actionDown && !prevActionDown;
            prevActionDown = actionDown;

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
                case State.Lost:
                    if (actionPressed) NewGame();
                    break;
            }
            base.Update(gameTime);
        }

        private void MovePaddle(KeyboardState keys, MouseState mouse, TouchCollection touches, float dt)
        {
            float move = 0;
            if (keys.IsKeyDown(Keys.Left) || keys.IsKeyDown(Keys.A)) move -= PaddleSpeed * dt;
            if (keys.IsKeyDown(Keys.Right) || keys.IsKeyDown(Keys.D)) move += PaddleSpeed * dt;
            var pad = GamePad.GetState(PlayerIndex.One);
            move += pad.ThumbSticks.Left.X * PaddleSpeed * dt;

            int x = paddle.X + (int)move;
            if (touches.Count > 0)
                x = (int)(touches[0].Position.X * ScreenWidth / GraphicsDevice.Viewport.Width) - paddle.Width / 2;
            else if (mouse.X != lastMouseX)
                x = (int)(mouse.X * (float)ScreenWidth / GraphicsDevice.Viewport.Width) - paddle.Width / 2;
            lastMouseX = mouse.X;
            paddle.X = MathHelper.Clamp(x, 0, ScreenWidth - paddle.Width);
        }

        private int lastMouseX;

        private void UpdateBall(float dt)
        {
            ballPosition += ballVelocity * dt;

            // Walls
            if (ballPosition.X < 0) { ballPosition.X = 0; ballVelocity.X = Math.Abs(ballVelocity.X); PlaySound("WallHit"); }
            if (ballPosition.X + BallSize > ScreenWidth)
            {
                ballPosition.X = ScreenWidth - BallSize;
                ballVelocity.X = -Math.Abs(ballVelocity.X);
                PlaySound("WallHit");
            }
            if (ballPosition.Y < 0) { ballPosition.Y = 0; ballVelocity.Y = Math.Abs(ballVelocity.Y); PlaySound("WallHit"); }

            var ball = new Rectangle((int)ballPosition.X, (int)ballPosition.Y, BallSize, BallSize);

            // Paddle: bounce angle depends on where the ball hits.
            if (ballVelocity.Y > 0 && ball.Intersects(paddle))
            {
                float offset = (ball.Center.X - paddle.Center.X) / (paddle.Width / 2f);
                offset = MathHelper.Clamp(offset, -0.9f, 0.9f);
                float speed = ballVelocity.Length();
                ballVelocity = new Vector2(offset, -(float)Math.Sqrt(1 - offset * offset)) * speed;
                ballPosition.Y = paddle.Top - BallSize;
                PlaySound("PaddleHit");
            }

            // Bricks
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
                PlaySound("BrickBreak");
                break;
            }

            if (bricks.Count == 0)
            {
                state = State.Won;
                PlaySound("Win");
            }
            else if (ballPosition.Y > ScreenHeight)
            {
                lives--;
                PlaySound("LoseLife");
                if (lives <= 0) { state = State.Lost; PlaySound("Lose"); }
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

            for (int i = 0; i < bricks.Count; i++)
                Fill(bricks[i], brickColors[i]);
            Fill(paddle, Color.White);
            Fill(new Rectangle((int)ballPosition.X, (int)ballPosition.Y, BallSize, BallSize), Color.Cyan);

            spriteBatch.DrawString(font, $"Score: {score}", new Vector2(10, 10), Color.White);
            spriteBatch.DrawString(font, $"Lives: {lives}", new Vector2(ScreenWidth - 110, 10), Color.White);

            string message = state switch
            {
                State.Ready => "Press Space / click / tap to launch",
                State.Won => "You win! Press Space to play again",
                State.Lost => "Game over! Press Space to play again",
                _ => null
            };
            if (message != null)
            {
                var size = font.MeasureString(message);
                spriteBatch.DrawString(font, message,
                    new Vector2((ScreenWidth - size.X) / 2f, ScreenHeight / 2f), Color.White);
            }

            spriteBatch.End();
            base.Draw(gameTime);
        }

        private void Fill(Rectangle r, Color c) => spriteBatch.Draw(pixel, r, c);
    }
}
