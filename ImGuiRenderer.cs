// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vector2 = System.Numerics.Vector2;

namespace Celeste.Mod.ImGuiHelper;

/// <summary>
/// ImGui renderer for use with XNA-likes (FNA & MonoGame)
/// Majority borrowed from ImGui.NET samples.
/// https://github.com/ImGuiNET/ImGui.NET/blob/master/src/ImGui.NET.SampleProgram.XNA/ImGuiRenderer.cs
/// </summary>
public sealed class ImGuiRenderer {

    // Graphics
    private readonly GraphicsDevice graphicsDevice;

    private BasicEffect effect;
    private readonly RasterizerState rasterizerState;

    private byte[] vertexData;
    private VertexBuffer vertexBuffer;
    private int vertexBufferSize;

    private byte[] indexData;
    private IndexBuffer indexBuffer;
    private int indexBufferSize;

    // Textures
    private readonly Dictionary<IntPtr, Texture2D> loadedTextures;

    private int textureId;
    private IntPtr? fontTextureId;

    // Input
    private int scrollWheelValue;

    private readonly Keys[] allKeys = Enum.GetValues<Keys>();

    public ImGuiRenderer(Game game) {
        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        graphicsDevice = game.GraphicsDevice;

        loadedTextures = new Dictionary<IntPtr, Texture2D>();

        rasterizerState = new RasterizerState {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0
        };

        EnableDocking();
        SetupInput();
    }

    #region ImGuiRenderer

    /// <summary>
    /// Creates a texture and loads the font data from ImGui. Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done
    /// </summary>
    public unsafe void RebuildFontAtlas() {
        // Get font texture from ImGui
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

        // Copy the data to a managed array
        var pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);

        // Create and register the texture as an XNA texture
        var tex2d = new Texture2D(graphicsDevice, width, height, false, SurfaceFormat.Color);
        tex2d.SetData(pixels);

        // Should a texture already have been build previously, unbind it first so it can be deallocated
        if (fontTextureId.HasValue) UnbindTexture(fontTextureId.Value);

        // Bind the new texture to an ImGui-friendly id
        fontTextureId = BindTexture(tex2d);

        // Let ImGui know where to find the texture
        io.Fonts.SetTexID(fontTextureId.Value);
        io.Fonts.ClearTexData(); // Clears CPU side texture data
    }

    /// <summary>
    /// Creates a pointer to a texture, which can be passed through ImGui calls such as <see cref="ImGui.Image" />. That pointer is then used by ImGui to let us know what texture to draw
    /// </summary>
    public IntPtr BindTexture(Texture2D texture) {
        var id = new IntPtr(textureId++);

        loadedTextures.Add(id, texture);

        return id;
    }

    /// <summary>
    /// Removes a previously created texture pointer, releasing its reference and allowing it to be deallocated
    /// </summary>
    public void UnbindTexture(IntPtr textureId) {
        loadedTextures.Remove(textureId);
    }

    /// <summary>
    /// Sets up ImGui for a new frame, should be called at frame start
    /// </summary>
    public void BeforeLayout(float totalSeconds) {
        ImGui.GetIO().DeltaTime = totalSeconds;

        UpdateInput();

        ImGui.NewFrame();
        ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode);
    }

    /// <summary>
    /// Asks ImGui for the generated geometry data and sends it to the graphics pipeline, should be called after the UI is drawn using ImGui.** calls
    /// </summary>
    public void AfterLayout() {
        ImGui.Render();

        RenderDrawData(ImGui.GetDrawData());
    }

    #endregion ImGuiRenderer

    #region Setup & Update

    private void EnableDocking() {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        io.ConfigDockingAlwaysTabBar = true;
        io.ConfigDockingTransparentPayload = true;
    }

    /// <summary>
    /// Setup key input event handler
    /// </summary>
    private void SetupInput() {
        TextInput.OnInput += c => {
            if (c == '\t') return;

            ImGui.GetIO().AddInputCharacter(c);
        };

        ImGui.GetIO().Fonts.AddFontDefault();
    }

    /// <summary>
    /// Updates the <see cref="Effect" /> to the current matrices and texture
    /// </summary>
    private Effect UpdateEffect(Texture2D texture) {
        effect ??= new BasicEffect(graphicsDevice);

        var io = ImGui.GetIO();

        effect.World = Matrix.Identity;
        effect.View = Matrix.Identity;
        effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
        effect.TextureEnabled = true;
        effect.Texture = texture;
        effect.VertexColorEnabled = true;

        return effect;
    }

    /// <summary>
    /// Sends XNA input state to ImGui
    /// </summary>
    private void UpdateInput() {
        var io = ImGui.GetIO();

        io.DisplaySize = new System.Numerics.Vector2(graphicsDevice.PresentationParameters.BackBufferWidth,
            graphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);

        // if the game isn't focused, reset everything
        if (!Engine.Instance.IsActive) {
            io.ClearInputMouse();
            io.ClearInputKeys();
        } else {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();

            foreach (var key in allKeys) {
                if (TryMapKeys(key, out var imGuiKey)) {
                    io.AddKeyEvent(imGuiKey, keyboard.IsKeyDown(key));
                }
            }

            io.AddMousePosEvent(mouse.X, mouse.Y);
            io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
            io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

            int scrollDelta = mouse.ScrollWheelValue - scrollWheelValue;
            io.AddMouseWheelEvent(0, scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0);
            scrollWheelValue = mouse.ScrollWheelValue;
        }
    }

    /// <summary>
    /// Tries to map an FNA key to an ImGui key.
    /// </summary>
    private static bool TryMapKeys(Keys key, out ImGuiKey imGuiKey)
    {
        // Special case not handed in the switch...
        // If the actual key we put in is "None", return None and true.
        // Otherwise, return None and false.
        if (key == Keys.None)
        {
            imGuiKey = ImGuiKey.None;
            return true;
        }

        imGuiKey = key switch
        {
            Keys.Back => ImGuiKey.Backspace,
            Keys.Tab => ImGuiKey.Tab,
            Keys.Enter => ImGuiKey.Enter,
            Keys.CapsLock => ImGuiKey.CapsLock,
            Keys.Escape => ImGuiKey.Escape,
            Keys.Space => ImGuiKey.Space,
            Keys.PageUp => ImGuiKey.PageUp,
            Keys.PageDown => ImGuiKey.PageDown,
            Keys.End => ImGuiKey.End,
            Keys.Home => ImGuiKey.Home,
            Keys.Left => ImGuiKey.LeftArrow,
            Keys.Right => ImGuiKey.RightArrow,
            Keys.Up => ImGuiKey.UpArrow,
            Keys.Down => ImGuiKey.DownArrow,
            Keys.PrintScreen => ImGuiKey.PrintScreen,
            Keys.Insert => ImGuiKey.Insert,
            Keys.Delete => ImGuiKey.Delete,
            >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
            >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
            >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
            Keys.Multiply => ImGuiKey.KeypadMultiply,
            Keys.Add => ImGuiKey.KeypadAdd,
            Keys.Subtract => ImGuiKey.KeypadSubtract,
            Keys.Decimal => ImGuiKey.KeypadDecimal,
            Keys.Divide => ImGuiKey.KeypadDivide,
            >= Keys.F1 and <= Keys.F24 => ImGuiKey.F1 + (key - Keys.F1),
            Keys.NumLock => ImGuiKey.NumLock,
            Keys.Scroll => ImGuiKey.ScrollLock,
            Keys.LeftShift or Keys.RightShift => ImGuiKey.ModShift,
            Keys.LeftControl or Keys.RightControl => ImGuiKey.ModCtrl,
            Keys.LeftAlt or Keys.RightAlt => ImGuiKey.ModAlt,
            Keys.LeftWindows or Keys.RightWindows => ImGuiKey.ModSuper,
            Keys.OemSemicolon => ImGuiKey.Semicolon,
            Keys.OemPlus => ImGuiKey.Equal,
            Keys.OemComma => ImGuiKey.Comma,
            Keys.OemMinus => ImGuiKey.Minus,
            Keys.OemPeriod => ImGuiKey.Period,
            Keys.OemQuestion => ImGuiKey.Slash,
            Keys.OemTilde => ImGuiKey.GraveAccent,
            Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
            Keys.OemCloseBrackets => ImGuiKey.RightBracket,
            Keys.OemPipe => ImGuiKey.Backslash,
            Keys.OemQuotes => ImGuiKey.Apostrophe,
            _ => ImGuiKey.None,
        };

        return imGuiKey != ImGuiKey.None;
    }

    #endregion Setup & Update

    #region Internals

    /// <summary>
    /// Gets the geometry as set up by ImGui and sends it to the graphics device
    /// </summary>
    private void RenderDrawData(ImDrawDataPtr drawData) {
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
        var lastViewport = graphicsDevice.Viewport;
        var lastScissorBox = graphicsDevice.ScissorRectangle;

        graphicsDevice.BlendFactor = Color.White;
        graphicsDevice.BlendState = BlendState.NonPremultiplied;
        graphicsDevice.RasterizerState = rasterizerState;
        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        // Setup projection
        graphicsDevice.Viewport = new Viewport(0, 0, graphicsDevice.PresentationParameters.BackBufferWidth, graphicsDevice.PresentationParameters.BackBufferHeight);

        UpdateBuffers(drawData);

        RenderCommandLists(drawData);

        // Restore modified state
        graphicsDevice.Viewport = lastViewport;
        graphicsDevice.ScissorRectangle = lastScissorBox;
    }

    private unsafe void UpdateBuffers(ImDrawDataPtr drawData) {
        if (drawData.TotalVtxCount == 0) {
            return;
        }

        // Expand buffers if we need more room
        if (drawData.TotalVtxCount > vertexBufferSize) {
            vertexBuffer?.Dispose();

            vertexBufferSize = (int) (drawData.TotalVtxCount * 1.5f);
            vertexBuffer = new VertexBuffer(graphicsDevice, DrawVertDeclaration.Declaration, vertexBufferSize, BufferUsage.None);
            vertexData = new byte[vertexBufferSize * DrawVertDeclaration.Size];
        }

        if (drawData.TotalIdxCount > indexBufferSize) {
            indexBuffer?.Dispose();

            indexBufferSize = (int) (drawData.TotalIdxCount * 1.5f);
            indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indexBufferSize, BufferUsage.None);
            indexData = new byte[indexBufferSize * sizeof(ushort)];
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++) {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            fixed (void* vtxDstPtr = &vertexData[vtxOffset * DrawVertDeclaration.Size])
            fixed (void* idxDstPtr = &indexData[idxOffset * sizeof(ushort)]) {
                Buffer.MemoryCopy((void*) cmdList.VtxBuffer.Data, vtxDstPtr, vertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                Buffer.MemoryCopy((void*) cmdList.IdxBuffer.Data, idxDstPtr, indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the gpu vertex- and index buffers
        vertexBuffer.SetData(vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
        indexBuffer.SetData(indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private void RenderCommandLists(ImDrawDataPtr drawData) {
        graphicsDevice.SetVertexBuffer(vertexBuffer);
        graphicsDevice.Indices = indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++) {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++) {
                ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];

                if (drawCmd.ElemCount == 0) {
                    continue;
                }

                if (!loadedTextures.ContainsKey(drawCmd.TextureId)) {
                    throw new InvalidOperationException($"Could not find a texture with id '{drawCmd.TextureId}', please check your bindings");
                }

                graphicsDevice.ScissorRectangle = new Rectangle(
                    (int) drawCmd.ClipRect.X,
                    (int) drawCmd.ClipRect.Y,
                    (int) (drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                    (int) (drawCmd.ClipRect.W - drawCmd.ClipRect.Y)
                );

                var effect = UpdateEffect(loadedTextures[drawCmd.TextureId]);

                foreach (var pass in effect.CurrentTechnique.Passes) {
                    pass.Apply();

#pragma warning disable CS0618 // // FNA does not expose an alternative method.
                    graphicsDevice.DrawIndexedPrimitives(
                        primitiveType: PrimitiveType.TriangleList,
                        baseVertex: (int) drawCmd.VtxOffset + vtxOffset,
                        minVertexIndex: 0,
                        numVertices: cmdList.VtxBuffer.Size,
                        startIndex: (int) drawCmd.IdxOffset + idxOffset,
                        primitiveCount: (int) drawCmd.ElemCount / 3
                    );
#pragma warning restore CS0618
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    #endregion Internals
}
