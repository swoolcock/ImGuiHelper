using Celeste.Mod.ImGuiHelper.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Collections.Generic;

namespace Celeste.Mod.ImGuiHelper.Utilities;

public class ImGuiManager {
    private readonly ImGuiRenderer _renderer;
    private readonly RenderTarget2D _renderTarget;

    public static List<ImGuiComponent> GlobalComponents { get; } = new List<ImGuiComponent>();

    public ImGuiManager() {
        _renderer = new ImGuiRenderer(Engine.Instance);
        _renderer.RebuildFontAtlas();
        _renderTarget = new RenderTarget2D(Engine.Instance.GraphicsDevice, Engine.Width, Engine.Height, false, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.DiscardContents);
    }

    public void BeforeRender(float elapsedTime) {
        if (_renderTarget.IsDisposed) return;

        Engine.Graphics.GraphicsDevice.SetRenderTarget(_renderTarget);
        Engine.Graphics.GraphicsDevice.Clear(Color.Transparent);

        _renderer.BeforeLayout(elapsedTime);
        if (Engine.Scene?.Tracker.GetComponents<ImGuiComponent>() is { } components) {
            foreach (ImGuiComponent component in GlobalComponents) {
                component.Build?.Invoke(this);
            }
            foreach (ImGuiComponent component in components) {
                component.Build?.Invoke(this);
            }
        }
        _renderer.AfterLayout();
    }

    public void Render() {
        Draw.SpriteBatch.Begin();
        Draw.SpriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
        Draw.SpriteBatch.End();
    }
}
