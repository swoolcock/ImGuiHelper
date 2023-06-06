using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.ImGuiHelper;

public class ImGuiManager {
    private readonly ImGuiRenderer renderer;
    private RenderTarget2D renderTarget;

    public static List<ImGuiHandler> Handlers { get; } = new List<ImGuiHandler>();

    public ImGuiManager() {
        renderer = new ImGuiRenderer(Engine.Instance);
        renderer.RebuildFontAtlas();
    }

    public void RenderHandlers(float rawDeltaTime) {
        if (renderTarget == null || renderTarget.IsDisposed || renderTarget.Width != Engine.Width || renderTarget.Height != Engine.Height) {
            renderTarget?.Dispose();
            renderTarget = new RenderTarget2D(Engine.Instance.GraphicsDevice, Engine.Width, Engine.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
        }

        Engine.Graphics.GraphicsDevice.SetRenderTarget(renderTarget);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

        renderer.BeforeLayout(rawDeltaTime);
        foreach (var handler in Handlers) {
            handler.Render();
        }
        renderer.AfterLayout();
    }

    public void RenderTexture() {
        Draw.SpriteBatch.Begin();
        Draw.SpriteBatch.Draw(renderTarget, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }

    public void UpdateHandlers(GameTime gameTime) {
        foreach (var handler in Handlers) {
            handler.Update(gameTime);
        }
    }
}
