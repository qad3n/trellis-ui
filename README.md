# Trellis

IMGUI menu framework for Unity IL2CPP games modded with BepInEx 6 and Il2CppInterop. You get a themed, resizable window with tabs, block layout, widgets, popups and tooltips.

## Using it

Trellis is compiled into your plugin from source:

```xml
<ItemGroup>
  <Using Include="Trellis" />
  <Compile Include="../trellis/source/**/*.cs" Link="trellis/%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

Bind a logger once at startup. The other hooks (`UiFont`, `UiImages`, `SliderGuard`, `Live`) are optional.

```csharp
UiLog.bind(debug, info, warn, trace, logException, debugEnabled);
```

Then call it from your own `Update` and `OnGUI`:

```csharp
if (Input.GetKeyDown(KeyCode.F1))
    MenuHost.toggle();
MenuHost.update(true);

MenuHost.cacheInput();
MenuHost.onGUI();
```

A menu is tabs, each tab is a list of blocks, and a block is a draw function:

```csharp
internal enum Block { Options }

MenuHost.setTitle("<b>MY MOD</b>", "v1.0");
MenuHost.addBlocks(new[] { (Block.Options, (BlockDraw)drawOptions) });
MenuHost.setTabs(new[] { new TabRow("MAIN", (w, viewH) => MenuHost.drawBlocks(w, 0f, kMain)) });

private static void drawOptions(ref float y, float w)
{
    m_enabled = Widgets.drawToggle(ref y, w, m_enabled, "Enabled");
    m_speed = Widgets.drawSlider(ref y, w, "Speed", m_speed, 0f, 4f);
}
```

`kMain` is a static `Block[]`. `sample/` has a complete plugin.

Settings are static fields marked `[Config]`. Trellis never touches the disk, so your config system has to load and save them. Keep the `[Obfuscation]` attribute on any type that owns one, or obfuscation will rename the type and break its key.

## Building

Put your interop DLLs in `refs/core` and `refs/interop`, or pass `-p:UnityRefs=<dir>`.

```bash
dotnet build Trellis.csproj
```
