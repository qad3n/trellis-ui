# Trellis

Trellis is an immediate mode (IMGUI) menu framework for Unity IL2CPP games modded through BepInEx 6 and Il2CppInterop. It draws a resizable, themed window with a sidebar of tabs. Each tab holds pages of blocks, and blocks pack themselves into columns. On top of that it provides animated widgets, popups, tooltips and text editing, all routed through allocation-free helpers built for the IL2CPP interop cost model.

The library has no knowledge of any particular game or mod. Anything a host owns (fonts, files, logging, persistence, game input) comes in through a small set of hooks.

## Requirements

- .NET 6, C# latest
- BepInEx 6 IL2CPP with Il2CppInterop
- A Unity interop set with `UnityEngine.CoreModule`, `IMGUIModule`, `InputLegacyModule`, `TextRenderingModule` and `UnityEngine.UI`

## How a host uses it

Trellis ships as source. A host compiles `source/` straight into its own plugin assembly instead of loading a second DLL. That keeps one assembly to protect, obfuscate and load, and the host's reflection-based config loader sees the library's settings.

```xml
<ItemGroup>
  <Using Include="Trellis" />
  <Compile Include="../trellis/source/**/*.cs" Link="trellis/%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

Then wire the host services once, before the first frame. This is the whole contract:

```csharp
UiLog.bind(debug, info, warn, trace, logException, debugEnabled); // every library log line goes to your logger
UiFont.bind(loadFont, resolvedFont);                               // optional, falls back to the IMGUI default font
UiImages.bind(listImages, tryLoadImage, folderHint);               // optional, window background images
SliderGuard.bind(flush, release, noteUnsafe, trySafeDefault);      // optional, values typed past a slider range
RichText.nameLimits(maxMarkup, maxVisible);                        // optional, display name caps
Live.layout(cachedPtrOffset, instanceIdOffset, frameCountIcall);   // optional native fast paths, then
Live.enable(liveness, instanceId, frames);                         // enable only what you self tested
```

Drive it from your own Unity hooks:

```csharp
// Update
if (Input.GetKeyDown(toggleKey))
    MenuHost.toggle();
MenuHost.update(captureInput: true);

// OnGUI
MenuHost.cacheInput();
MenuHost.onGUI();
```

`sample/` is a complete minimal plugin that does exactly this.

## Building a menu

A menu is data. Tabs are rows, pages are block lists, and blocks are draw functions.

```csharp
internal enum Block { Welcome, Options }

private static readonly (Block id, BlockDraw draw)[] kBlocks = { (Block.Welcome, drawWelcome), (Block.Options, drawOptions) };
private static readonly Block[] kMain = { Block.Welcome, Block.Options };

MenuHost.setTitle("<b>MY MOD</b>", "v1.0");
MenuHost.addBlocks(kBlocks);
MenuHost.setTabs(new[] { new TabRow("MAIN", (w, viewH) => MenuHost.drawBlocks(w, 0f, kMain)) });

private static void drawOptions(ref float y, float w)
{
    Widgets.drawSectionHeader(ref y, w, "Options");
    m_enabled = Widgets.drawToggle(ref y, w, m_enabled, "Enabled");
    m_speed = Widgets.drawSlider(ref y, w, "Speed", m_speed, 0f, 4f);
}
```

- **Blocks** keep their identity across frames through your enum, so layout, animation and measured heights stay stable. Block lists must be static tables; each list is converted once and held for as long as the table lives.
- **Columns:** `MenuHost.drawColumns` pins blocks to declared columns. `SubTabHost.forBlocks` builds a page with sub tabs over block lists.
- **Popups:** `MenuHost.showConfirm` and `MenuHost.showInfo` are built in. A host popup joins the dim, dismiss, close and draw passes through `MenuHost.addPopup(new PopupRow(...))`.
- **Tabs that scroll themselves** pass `ownsScroll` on their `TabRow`.
- **Input gating:** `MenuHost.addClaim` registers any host overlay that should also keep clicks away from the game.
- **Text selection:** each selectable surface holds a `TextSelectionOwner`. Claiming focus clears whoever held it.
- **Theme extension:** a host palette follows the theme with `Theme.addRebuild` and `Theme.addCleanup`.

## Settings

Library settings are static fields marked `[Config]`. Trellis does not read or write files. The host's config system finds the fields by reflection and keys them as `Type.FullName.field` (for example `Trellis.ThemeConfig.CornerRadius`). Every type that owns a `[Config]` field carries `[Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]` so its key survives a renaming obfuscator. Keep that attribute when you add a setting.

## Rules the code follows

- **Main thread only.** Every entry point is a Unity callback.
- **Hot paths do not allocate.** `OnGUI`, `Update` and every widget avoid allocation, LINQ and throwaway strings. Measured text is cached.
- **Interop calls go through helpers.** Every Unity call is an interop invoke, so GUI state goes through `Gui`, vector and colour math through `Math/`, and liveness through `Live`.
- **No reach of its own.** The library has no file, network, process or console access. It never loads assemblies, patches methods or runs code before the host calls in. `tools/guard-lint.sh` enforces this.

## Layout

```
source/
  Host/      the hooks a host binds (log, font, images, slider guard, liveness, casts)
  Menu/      MenuHost window, tabs, input, popups, tooltips, resize, block layout, animation
  Theme/     palette, styles, surfaces, tint, colour picker, window chrome and background
  Widgets/   Display, Input (with Text), Layout
  Motion/    springs, fades, motion tokens
  Render/    Gui, Textures, OwnedContent, GraphicsInfo, ImagePreview
  Math/      interop free math
  Text/      ColorHex, RichText, ByteSize
  Font/      glyph prewarm cache
  Runtime/   EvictingCache, RowTable, ConfigAttribute
sample/      minimal BepInEx host
tools/       style, render, row flow and guard lints
```

## Building and linting

Copy or link your interop set to `refs/core` and `refs/interop` (ignored by git), or pass `-p:UnityRefs=<dir>`:

```bash
dotnet build Trellis.csproj -c Release
dotnet build sample/Sample.csproj -c Release
tools/guard-lint.sh && tools/style-lint.sh && tools/render-lint.sh && tools/anim-lint.sh
```

The standalone build only proves the library compiles without a host. Hosts compile `source/` themselves. The style, render and row flow lints take extra source directories as arguments, so a host can hold its own code to the same rules.
