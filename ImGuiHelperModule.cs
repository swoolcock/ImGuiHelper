using Celeste.Mod.ImGuiHelper.Entities;
using System;
using Celeste.Mod.ImGuiHelper.Utilities;
using ImGuiNET;
using Monocle;
using System.Linq;

namespace Celeste.Mod.ImGuiHelper;

public class ImGuiHelperModule : EverestModule {
    public static ImGuiHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(ImGuiHelperModuleSettings);
    public static ImGuiHelperModuleSettings Settings => (ImGuiHelperModuleSettings) Instance._Settings;

    public override Type SessionType => typeof(ImGuiHelperModuleSession);
    public static ImGuiHelperModuleSession Session => (ImGuiHelperModuleSession) Instance._Session;

    private static ImGuiManager _imGuiManager;

    public ImGuiHelperModule() {
        Instance = this;
    }

    public override void Load() {
        On.Monocle.Engine.RenderCore += Engine_RenderCore;
    }

    public override void Unload() {
        On.Monocle.Engine.RenderCore -= Engine_RenderCore;
    }

    public override void Initialize() {
        _imGuiManager = new ImGuiManager();
    }

    private void Engine_RenderCore(On.Monocle.Engine.orig_RenderCore orig, Engine self) {
        _imGuiManager?.BeforeRender(Engine.RawDeltaTime);
        orig(self);
        _imGuiManager?.Render();
    }
        
    [Command("imgui", "Test ImGui")]
    private static void CmdTestImGui() {
        if (!ImGuiManager.GlobalComponents.Any()) {
            ImGuiManager.GlobalComponents.Add(new ImGuiComponent(_ => {
                ImGui.ShowDemoWindow();
            }));
        }
    }
}