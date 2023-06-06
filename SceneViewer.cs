using ImGuiNET;
using Monocle;
using System.Numerics;

namespace Celeste.Mod.ImGuiHelper;

public class SceneViewer : ImGuiHandler {
    public override void Render() {
        var scene = Engine.Scene;
        ImGui.Begin("Scene");

        if (ImGui.BeginTable($"Scene ({scene.GetType().Name})", 5)) {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Depth", ImGuiTableColumnFlags.WidthFixed, 75);
            ImGui.TableSetupColumn("V", ImGuiTableColumnFlags.WidthFixed, 10);
            ImGui.TableSetupColumn("A", ImGuiTableColumnFlags.WidthFixed, 10);
            ImGui.TableSetupColumn("C", ImGuiTableColumnFlags.WidthFixed, 10);
            ImGui.TableHeadersRow();

            var whiteColor = new Vector4(1f);
            var purpleColor = new Vector4(185 / 255f, 149 / 255f, 248 / 255f, 1f);
            var greenColor = new Vector4(0f, 1f, 0f, 1f);
            var yellowColor = new Vector4(1f, 1f, 0f, 1f);

            foreach (var entity in scene.Entities) {
                var color = entity switch {
                    Player or PlayerDeadBody => greenColor,
                    Solid => purpleColor,
                    Decal => yellowColor,
                    _ => whiteColor,
                };

                if (!entity.Visible) color.W *= 0.5f;

                ImGui.PushStyleColor(ImGuiCol.Text, color);
                ImGui.TableNextColumn();
                ImGui.Text(entity.GetType().Name);
                ImGui.TableNextColumn();
                ImGui.Text(((DepthsEnum) entity.Depth).ToString());
                ImGui.PopStyleColor();
                ImGui.TableNextColumn();
                if (entity.Visible) ImGui.Text("X");
                ImGui.TableNextColumn();
                if (entity.Active) ImGui.Text("X");
                ImGui.TableNextColumn();
                if (entity.Collidable) ImGui.Text("X");
            }
            ImGui.EndTable();
        }

        ImGui.End();
    }

    public enum DepthsEnum {
        BGTerrain = 10000,
        BGMirrors = 9500,
        BGDecals = 9000,
        BGParticles = 8000,
        SolidsBelow = 5000,
        Below = 2000,
        NPCs = 1000,
        TheoCrystal = 100,
        Player = 0,
        Dust = -50,
        Pickups = -100,
        Seeker = -200,
        Particles = -8000,
        Above = -8500,
        Solids = -9000,
        FGTerrain = -10000,
        FGDecals = -10500,
        DreamBlocks = -11000,
        CrystalSpinners = -11500,
        PlayerDreamDashing = -12000,
        Enemy = -12500,
        FakeWalls = -13000,
        FGParticles = -50000,
        Top = -1000000,
        FormationSequences = -2000000,
    }
}
