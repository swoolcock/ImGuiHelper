// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System.Linq;

namespace Celeste.Mod.ImGuiHelper;

public class ImGuiHelperModule : EverestModule {
    public static ImGuiHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(ImGuiHelperModuleSettings);
    public static ImGuiHelperModuleSettings Settings => (ImGuiHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(ImGuiHelperModuleSession);
    public static ImGuiHelperModuleSession Session => (ImGuiHelperModuleSession) Instance._Session;

    private static ImGuiManager imGuiManager;

    public ImGuiHelperModule() {
        Instance = this;
    }

    public override void Load() {
        On.Monocle.Engine.RenderCore += Engine_RenderCore;
        On.Monocle.Engine.Update += Engine_Update;
        On.Monocle.Commands.UpdateOpen += Commands_UpdateOpen;
        On.Monocle.Commands.UpdateClosed += Commands_UpdateClosed;
        On.Monocle.Commands.HandleChar += Commands_HandleChar;
    }

    public override void Unload() {
        On.Monocle.Engine.RenderCore -= Engine_RenderCore;
        On.Monocle.Engine.Update -= Engine_Update;
        On.Monocle.Commands.UpdateOpen -= Commands_UpdateOpen;
        On.Monocle.Commands.UpdateClosed -= Commands_UpdateClosed;
        On.Monocle.Commands.HandleChar -= Commands_HandleChar;
    }

    public override void Initialize() {
        imGuiManager = new ImGuiManager();
        Engine.Instance.IsMouseVisible = Settings.ShowMouseCursorOnStartup;
    }

    private static void Engine_RenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        imGuiManager?.RenderHandlers(Engine.RawDeltaTime);

        orig(self);

        var oldViewport = Engine.Instance.GraphicsDevice.Viewport;
        Engine.Instance.GraphicsDevice.Viewport = new Viewport(0, 0, Engine.Instance.Window.ClientBounds.Width, Engine.Instance.Window.ClientBounds.Height);
        imGuiManager?.RenderTexture();
        Engine.Instance.GraphicsDevice.Viewport = oldViewport;
    }

    private static void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gametime) {
        var disableInput = imGuiManager?.WantCaptureKeyboard ?? false;
        MInput.Disabled = disableInput;
        MInput.Active = !disableInput;
        Engine.Commands.Enabled = !disableInput;

        orig(self, gametime);

        if (Settings.ToggleMouseCursor.Pressed) {
            Engine.Instance.IsMouseVisible = !Engine.Instance.IsMouseVisible;
        }

        imGuiManager?.UpdateHandlers(gametime);
    }

    private void Commands_UpdateOpen(On.Monocle.Commands.orig_UpdateOpen orig, Monocle.Commands self) {
        if (imGuiManager?.WantCaptureKeyboard ?? false) return;
        orig(self);
    }

    private void Commands_UpdateClosed(On.Monocle.Commands.orig_UpdateClosed orig, Monocle.Commands self) {
        if (imGuiManager?.WantCaptureKeyboard ?? false) return;
        orig(self);
    }

    private void Commands_HandleChar(On.Monocle.Commands.orig_HandleChar orig, Monocle.Commands self, char key) {
        if (imGuiManager?.WantCaptureKeyboard ?? false) return;
        orig(self, key);
    }

    [Command("imgui_clear", "Remove all ImGui handlers")]
    private static void CmdImGuiClear() {
        ImGuiManager.Handlers.Clear();
    }

    [Command("imgui_demo", "Show ImGui Demo Window")]
    private static void CmdImGuiDemoWindow() {
        if (!ImGuiManager.Handlers.OfType<DemoWindow>().Any()) {
            ImGuiManager.Handlers.Add(new DemoWindow());
        }
    }

    private class DemoWindow : ImGuiHandler {
        public override void Render() => ImGui.ShowDemoWindow();
    }
}