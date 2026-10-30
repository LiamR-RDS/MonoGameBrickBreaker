using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace MonoGameBrickBreaker.Core;

public class MonoGameBrickBreakerGame : Game
{
    private enum Screen
    {
        MainMenu,
        LevelSelect,
        Editor,
        Game
    }

    private SpriteBatch spriteBatch;
    private Texture2D pixel;
    private SpriteFont font;
    private GameRenderer renderer;
    private LevelFileService levelFileService;
    private LevelDefinition[] levels;
    private MainMenuScreen mainMenu;
    private LevelSelectScreen levelSelect;
    private LevelEditorScreen editor;
    private GameplayScreen gameplay;
    private Screen screen = Screen.MainMenu;
    private KeyboardState previousKeyboardState;
    private bool previousActionDown;
    private bool previousMouseDown;
    private bool previousGamepadBackDown;

    public MonoGameBrickBreakerGame()
    {
        _ = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameConstants.ScreenWidth,
            PreferredBackBufferHeight = GameConstants.ScreenHeight,
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
        renderer = new GameRenderer(spriteBatch, pixel, font);
        levelFileService = new LevelFileService(Content.RootDirectory, GameConstants.TotalLevels);
        levels = levelFileService.LoadLevels();
        mainMenu = new MainMenuScreen();
        levelSelect = new LevelSelectScreen();
        editor = new LevelEditorScreen(levels, levelFileService);
        gameplay = new GameplayScreen(levels);
        previousKeyboardState = Keyboard.GetState();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keys = Keyboard.GetState();
        MouseState mouse = Mouse.GetState();
        TouchCollection touches = TouchPanel.GetState();
        GamePadState gamepad = GamePad.GetState(PlayerIndex.One);
        bool actionDown = keys.IsKeyDown(Keys.Space) || keys.IsKeyDown(Keys.Enter)
                                                     || mouse.LeftButton == ButtonState.Pressed || touches.Count > 0
                                                     || gamepad.Buttons.A == ButtonState.Pressed;
        bool actionPressed = actionDown && !previousActionDown;
        bool upPressed = WasKeyPressed(keys, Keys.Up) || WasKeyPressed(keys, Keys.W);
        bool downPressed = WasKeyPressed(keys, Keys.Down) || WasKeyPressed(keys, Keys.S);
        bool leftPressed = WasKeyPressed(keys, Keys.Left) || WasKeyPressed(keys, Keys.A);
        bool rightPressed = WasKeyPressed(keys, Keys.Right) || WasKeyPressed(keys, Keys.D);
        bool backPressed = WasKeyPressed(keys, Keys.Escape) ||
                           gamepad.Buttons.Back == ButtonState.Pressed && !previousGamepadBackDown;
        if (backPressed)
        {
            if (screen == Screen.Game || screen == Screen.MainMenu) Exit();
            else screen = Screen.MainMenu;
        }

        Point mousePoint = GetVirtualMousePosition(mouse);
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        switch (screen)
        {
            case Screen.MainMenu:
                UpdateMainMenu(actionPressed, upPressed, downPressed, mousePoint);
                break;
            case Screen.LevelSelect:
                UpdateLevelSelect(actionPressed, upPressed, downPressed, leftPressed, rightPressed, mousePoint);
                break;
            case Screen.Editor:
                if (editor.Update(actionPressed, upPressed, downPressed, leftPressed, rightPressed, keys,
                        mousePoint, mouse.LeftButton == ButtonState.Pressed, previousMouseDown,
                        gameTime.ElapsedGameTime.TotalSeconds))
                    screen = Screen.MainMenu;
                break;
            case Screen.Game:
                if (gameplay.Update(actionPressed, keys, mouse, touches, gamepad, dt, GraphicsDevice.Viewport.Width))
                    screen = Screen.MainMenu;
                break;
        }

        previousActionDown = actionDown;
        previousKeyboardState = keys;
        previousMouseDown = mouse.LeftButton == ButtonState.Pressed;
        previousGamepadBackDown = gamepad.Buttons.Back == ButtonState.Pressed;
        base.Update(gameTime);
    }

    private void UpdateMainMenu(bool actionPressed, bool upPressed, bool downPressed, Point mousePoint)
    {
        switch (mainMenu.Update(actionPressed, upPressed, downPressed, mousePoint))
        {
            case MainMenuAction.Play:
                gameplay.StartLevel(0, true);
                screen = Screen.Game;
                break;
            case MainMenuAction.LevelSelect:
                screen = Screen.LevelSelect;
                break;
            case MainMenuAction.Editor:
                screen = Screen.Editor;
                break;
            case MainMenuAction.Quit:
                Exit();
                break;
        }
    }

    private void UpdateLevelSelect(bool actionPressed, bool upPressed, bool downPressed, bool leftPressed,
        bool rightPressed, Point mousePoint)
    {
        int result = levelSelect.Update(actionPressed, upPressed, downPressed, leftPressed, rightPressed, mousePoint);
        if (result == -2)
            screen = Screen.MainMenu;
        else if (result >= 0)
        {
            gameplay.StartLevel(result, true);
            screen = Screen.Game;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 15, 35));
        spriteBatch.Begin(samplerState: SamplerState.PointClamp,
            transformMatrix: Matrix.CreateScale(
                GraphicsDevice.Viewport.Width / (float)GameConstants.ScreenWidth,
                GraphicsDevice.Viewport.Height / (float)GameConstants.ScreenHeight, 1f));
        switch (screen)
        {
            case Screen.MainMenu: mainMenu.Draw(renderer); break;
            case Screen.LevelSelect: levelSelect.Draw(renderer); break;
            case Screen.Editor: editor.Draw(renderer); break;
            case Screen.Game: gameplay.Draw(renderer); break;
        }

        spriteBatch.End();
        base.Draw(gameTime);
    }

    private Point GetVirtualMousePosition(MouseState mouse) => new(
        (int)(mouse.X * (float)GameConstants.ScreenWidth / GraphicsDevice.Viewport.Width),
        (int)(mouse.Y * (float)GameConstants.ScreenHeight / GraphicsDevice.Viewport.Height));

    private bool WasKeyPressed(KeyboardState keys, Keys key) =>
        keys.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
}