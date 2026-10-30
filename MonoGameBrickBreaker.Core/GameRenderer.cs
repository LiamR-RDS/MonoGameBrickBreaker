using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameBrickBreaker.Core;

public sealed class GameRenderer
{
    private readonly SpriteBatch spriteBatch;
    private readonly Texture2D pixel;
    private readonly SpriteFont font;

    public GameRenderer(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        this.spriteBatch = spriteBatch;
        this.pixel = pixel;
        this.font = font;
    }

    public void Fill(Rectangle rectangle, Color color) => spriteBatch.Draw(pixel, rectangle, color);

    public void DrawString(string text, Vector2 position, Color color, float scale = 1f) =>
        spriteBatch.DrawString(font, text, position, color, 0, Vector2.Zero, scale, SpriteEffects.None, 0);

    public void DrawCentered(string text, float y, Color color)
    {
        Vector2 size = font.MeasureString(text);
        DrawString(text, new Vector2((800 - size.X) / 2f, y), color);
    }

    public void DrawTitle(string text, float y, float scale)
    {
        Vector2 size = font.MeasureString(text) * scale;
        DrawString(text, new Vector2((800 - size.X) / 2f, y), Color.Cyan, scale);
    }

    public void DrawButton(Rectangle rectangle, string text, bool selected)
    {
        Fill(rectangle, selected ? new Color(35, 105, 150) : new Color(35, 35, 65));
        DrawOutline(rectangle, selected ? Color.Cyan : new Color(100, 100, 130), selected ? 2 : 1);
        Vector2 size = font.MeasureString(text);
        DrawString(text, new Vector2(rectangle.Center.X - size.X / 2f, rectangle.Center.Y - size.Y / 2f),
            Color.White);
    }

    public void DrawOutline(Rectangle rectangle, Color color, int thickness = 1)
    {
        Fill(new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
        Fill(new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
        Fill(new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
        Fill(new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
    }
}