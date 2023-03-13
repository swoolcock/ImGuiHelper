using Celeste.Mod.ImGuiHelper.Utilities;
using System;
using Monocle;

namespace Celeste.Mod.ImGuiHelper.Entities;

[Tracked]
public class ImGuiComponent : Component {
    public Action<ImGuiManager> Build { get; set; }

    public ImGuiComponent(Action<ImGuiManager> build = default) : base(false, true) {
        Build = build;
    }
}
