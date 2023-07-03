# ImGuiHelper
A Celeste helper mod that provides  [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET) bindings to other code helpers.

[Dear ImGui](https://github.com/ocornut/imgui) is a library that provides an [Immediate Mode Graphical User Interface](https://en.wikipedia.org/wiki/Immediate_mode_GUI) API for coders to design and draw simple overlays very quickly.
It's mostly used for tools and debugging, not for actual gameplay.

tl;dr: Fancy buttons and windows in Celeste.

### For code modders:
This **requires** the Core build of Everest, it will not run on the .NET Framework or Mono builds.

1) Add a package reference to ImGui.NET: `<PackageReference Include="ImGui.NET" Version="1.89.4" />`
2) Add an assembly reference to ImGuiHelper.dll (stripped, if you want).
3) Make one or more classes that inherit from `ImGuiHandler`.
4) In these classes, override the `Render` method.
   1) Call `ImGuiNET.ImGui.Begin("Some Window Name");`
   2) Make other calls to `ImGui` to create your interface.
   3) Call `ImGuiNET.ImGui.End();`
5) Register the handler classes by calling `ImGuiManager.Handlers.Add(someHandler);`.

* There is a simple mouse cursor that can be shown if desired (you probably do).  Be warned that I am not an artist.
* The entire GUI is drawn to a single render target *before* `Engine.Render`, and then that is drawn over the screen *after* `Engine.Render`.
* You can make as many windows in a single handler as you like, as long as the `Begin` and `End` calls are balanced.
* You should probably also ensure you never have more than one instance of a single handler registered.<br/>
  (Check with `ImGuiManager.Handlers.OfType<MyHandler>().Any()`)
* Windows don't *have* to be rendered; if you only want to display the window in certain situations then you can
  override the `Update` method in the handler to decide what to do.  This is executed after `Engine.Update` 
* Docking is enabled by default.

### For players:
You only need to install this if it's a dependency of something else. 

### Commands:
* `imguidemo` : Opens the default Dear ImGui demo window.
* `imguiclear` : Removes all handlers and their corresponding windows.
* `scene` : Opens a work-in-progress scene viewer that shows all entities in the current scene.

### Settings:
* **Show Mouse Cursor On Startup** (On/Off) : Defines whether the mouse cursor should display when the game starts, defaults to Off.
* **Toggle Mouse Cursor** (Binding) : Defines a hotkey to toggle the mouse cursor, unbound by default.~~~~
