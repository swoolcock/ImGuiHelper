using System;
using ImGuiNET;
using Microsoft.Xna.Framework;
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
    }

    public override void Unload() {
        On.Monocle.Engine.RenderCore -= Engine_RenderCore;
        On.Monocle.Engine.Update -= Engine_Update;
    }

    public override void Initialize() {
        imGuiManager = new ImGuiManager { ShowMouseCursor = Settings.ShowMouseCursorOnStartup };
    }

    private static void Engine_RenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        imGuiManager?.RenderHandlers(Engine.RawDeltaTime);
        orig(self);
        imGuiManager?.RenderTexture();
    }

    private static void Engine_Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gametime) {
        orig(self, gametime);

        if (Settings.ToggleMouseCursor.Pressed) {
            imGuiManager.ShowMouseCursor = !imGuiManager.ShowMouseCursor;
        }

        imGuiManager?.UpdateHandlers(gametime);
    }

    [Command("imguiclear", "Remove all ImGui handlers")]
    private static void CmdImGuiClear() {
        ImGuiManager.Handlers.Clear();
    }

    [Command("imguidemo", "Show ImGui Demo Window")]
    private static void CmdImGuiDemoWindow() {
        if (!ImGuiManager.Handlers.OfType<DemoWindow>().Any()) {
            ImGuiManager.Handlers.Add(new DemoWindow());
        }
    }

    [Command("scene", "Show ImGui Scene Graph")]
    private static void CmdImGuiSceneViewer() {
        if (!ImGuiManager.Handlers.OfType<SceneViewer>().Any()) {
            ImGuiManager.Handlers.Add(new SceneViewer());
        }
    }

    private class DemoWindow : ImGuiHandler {
        public override void Render() => ImGui.ShowDemoWindow();
    }
}