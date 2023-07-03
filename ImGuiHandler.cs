// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Microsoft.Xna.Framework;
using System;

namespace Celeste.Mod.ImGuiHelper;

public class ImGuiHandler {
    public Action RenderAction { get; set; }
    public Action<GameTime> UpdateAction { get; set; }

    public ImGuiHandler(Action renderAction = default, Action<GameTime> updateAction = default) {
        RenderAction = renderAction;
        UpdateAction = updateAction;
    }

    public virtual void Render() => RenderAction?.Invoke();

    public virtual void Update(GameTime gameTime) => UpdateAction?.Invoke(gameTime);
}