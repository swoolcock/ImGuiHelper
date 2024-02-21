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

    private readonly List<int> keys = new List<int>();

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
        ImGui.DockSpaceOverViewport(ImGui.GetMainViewport(), ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingInCentralNode);
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
    /// Maps ImGui keys to XNA keys. We use this later on to tell ImGui what keys were pressed
    /// </summary>
    private void SetupInput() {
        var io = ImGui.GetIO();

        keys.Add(io.KeyMap[(int) ImGuiKey.Tab] = (int) Keys.Tab);
        keys.Add(io.KeyMap[(int) ImGuiKey.LeftArrow] = (int) Keys.Left);
        keys.Add(io.KeyMap[(int) ImGuiKey.RightArrow] = (int) Keys.Right);
        keys.Add(io.KeyMap[(int) ImGuiKey.UpArrow] = (int) Keys.Up);
        keys.Add(io.KeyMap[(int) ImGuiKey.DownArrow] = (int) Keys.Down);
        keys.Add(io.KeyMap[(int) ImGuiKey.PageUp] = (int) Keys.PageUp);
        keys.Add(io.KeyMap[(int) ImGuiKey.PageDown] = (int) Keys.PageDown);
        keys.Add(io.KeyMap[(int) ImGuiKey.Home] = (int) Keys.Home);
        keys.Add(io.KeyMap[(int) ImGuiKey.End] = (int) Keys.End);
        keys.Add(io.KeyMap[(int) ImGuiKey.Delete] = (int) Keys.Delete);
        keys.Add(io.KeyMap[(int) ImGuiKey.Backspace] = (int) Keys.Back);
        keys.Add(io.KeyMap[(int) ImGuiKey.Enter] = (int) Keys.Enter);
        keys.Add(io.KeyMap[(int) ImGuiKey.Escape] = (int) Keys.Escape);
        keys.Add(io.KeyMap[(int) ImGuiKey.Space] = (int) Keys.Space);
        keys.Add(io.KeyMap[(int) ImGuiKey.A] = (int) Keys.A);
        keys.Add(io.KeyMap[(int) ImGuiKey.C] = (int) Keys.C);
        keys.Add(io.KeyMap[(int) ImGuiKey.V] = (int) Keys.V);
        keys.Add(io.KeyMap[(int) ImGuiKey.X] = (int) Keys.X);
        keys.Add(io.KeyMap[(int) ImGuiKey.Y] = (int) Keys.Y);
        keys.Add(io.KeyMap[(int) ImGuiKey.Z] = (int) Keys.Z);

        // MonoGame-specific //////////////////////
        // _game.Window.TextInput += (s, a) =>
        // {
        //     if (a.Character == '\t') return;
        //
        //     io.AddInputCharacter(a.Character);
        // };
        ///////////////////////////////////////////

        // FNA-specific ///////////////////////////
        TextInput.OnInput += c => {
            if (c == '\t') return;

            ImGui.GetIO().AddInputCharacter(c);
        };
        ///////////////////////////////////////////

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
            io.MousePos = new System.Numerics.Vector2(-1f);
            scrollWheelValue = 0;
            io.ClearInputKeys();
        } else {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();

            for (int i = 0; i < keys.Count; i++) {
                io.KeysDown[keys[i]] = keyboard.IsKeyDown((Keys) keys[i]);
            }

            io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
            io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

            io.MousePos = new System.Numerics.Vector2(mouse.X, mouse.Y);

            io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

            var scrollDelta = mouse.ScrollWheelValue - scrollWheelValue;
            io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
            scrollWheelValue = mouse.ScrollWheelValue;
        }
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
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];

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

    private unsafe void RenderCommandLists(ImDrawDataPtr drawData) {
        graphicsDevice.SetVertexBuffer(vertexBuffer);
        graphicsDevice.Indices = indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++) {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];

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
