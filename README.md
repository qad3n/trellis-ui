# Trellis

IMGUI menu framework for Unity IL2CPP games modded with BepInEx 6 and Il2CppInterop. You get a themed, resizable window with tabs, block layout, widgets, popups and tooltips, plus the on-screen layer around it: notifications, overlay text and standalone windows.

## Setup

Compile Trellis into your plugin from source:

```xml
<ItemGroup>
  <Using Include="Trellis" />
  <Compile Include="../trellis/source/**/*.cs" Link="trellis/%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

`Host.bind` is the whole host contract. Every field is optional except the log sinks.

```csharp
Host.bind(new Host.Bindings
{
    m_debug = log.LogDebug,
    m_info = log.LogInfo,
    m_warn = log.LogWarning,
    m_trace = log.LogDebug,
    m_exception = (where, ex) => log.LogError($"{where}: {ex}"),
    m_debugEnabled = () => false,
    m_loadFont = MyFonts.load,          // null falls back to the imgui default font
    m_resolvedFont = () => MyFonts.font,
    m_listImages = MyImages.list,       // background image picker
    m_loadImage = MyImages.tryLoad,
    m_imageHint = "put images in BepInEx/plugins/images",
    m_sliderFlush = MyConfig.applyPending,   // typed values past a slider range
    m_sliderRelease = MyConfig.release,
    m_sliderUnsafe = MyConfig.noteUnsafe,
    m_sliderDefault = MyConfig.trySafeDefault,
    m_inputClaimed = () => MyOverlay.hasInput,
    m_nameMarkupLimit = 1024,           // caps for RichText.displayName
    m_nameVisibleLimit = 31,
    m_badges = MyBadges.rows,           // see Badges
    m_levels = MyLog.levels,            // see Notifications
    m_notifyRef = MyPlayers.tryName
});
```

Drive it from your own MonoBehaviour. `cacheInput` has to run at the start of every `OnGUI`.

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.F1))
        MenuHost.toggle();

    MenuHost.update(true); // true keeps clicks away from the game while the menu is open
}

void OnGUI()
{
    MenuHost.cacheInput();
    MenuHost.onGUI();
    Notifications.onGUI();
}
```

## The API

| Class | What it owns |
|---|---|
| `Ui` | rows at the block cursor: widgets, scopes, pages |
| `UiAt` | the same widgets at a rect you own, plus chrome boxes |
| `UiScroll` | scroll state, bars, scroll regions and lists |
| `UiText` | measuring, drawing and formatting text |
| `Theme`, `ThemeConfig`, `ColorPicker` | palette, styles, boxes, knobs |
| `Gui`, `Textures`, `PixelCanvas`, `ShapeRaster` | drawing, input state, textures |
| `MenuHost`, `MenuAnimation`, `WindowHost` | the window, its passes, standalone windows |
| `Notifications`, `OverlayStyle`, `OverlayText` | the on-screen layer outside the menu |
| `Badges` | chips for the rows a host registers |
| `Host` | everything the host plugs in |

Everything else under `source/` is the framework's own plumbing.

## A first menu

A tab shows a page, a page is a list of blocks, and a block is a static method. Widgets stack down on their own.

```csharp
static bool m_enabled = true;
static float m_speed = 1f;

static readonly Page kMain = Ui.page(drawGeneral, drawAbout);

static void install()
{
    MenuHost.setTitle("<b>MY MOD</b>", "v1.0");
    MenuHost.setTabs(new[] { new TabRow("MAIN", kMain.draw) });
}

static void drawGeneral()
{
    Ui.header("General");
    Ui.toggle(ref m_enabled, "Enabled", "Shown as a tooltip.");
    Ui.slider(ref m_speed, "Speed", 0f, 4f);
}

static void drawAbout()
{
    Ui.header("About");
    Ui.note("Made with Trellis.");
}
```

Build pages once, in static fields. Blocks are laid out in as many columns as the window fits.

## Widgets

Widgets take the value by `ref` and return true on the frame it changes.

```csharp
if (Ui.toggle(ref m_esp, "ESP"))
    Esp.refresh();

Ui.slider(ref m_count, "Count", 1, 10); // an int field gets an int slider
Ui.keybind(ref m_key, "Hotkey"); // a KeyChord of up to four keys held together
Ui.textField(ref m_name, "Name");
Ui.toggleColor(ref m_glow, "Glow", "glow_color", ref m_glowColor);
Ui.button("Reset", reset);
Ui.info("Players", m_playerCount);
```

`ref` needs a field. A property won't compile, so copy it into a local and write it back.

A widget with a control on the trailing edge names them in draw order: `Ui.toggleDropdown`, `Ui.toggleDropdownColor`, `Ui.toggleDropdownColors`, `Ui.toggleMultiSelect`, `Ui.toggleMultiSelectColor`, `Ui.toggleCycle`, `Ui.toggleKey`.

Sliders come in units: `Ui.slider`, `Ui.sliderPx`, `Ui.sliderPercent`, `Ui.sliderMeters`, `Ui.sliderPerSecond`, `Ui.sliderUnits`, `Ui.sliderDistance`, and `Ui.sliderFull` when you want to write the readout yourself.

Enum dropdowns work when the enum counts up from 0 in the same order as the labels:

```csharp
enum Quality { Low, Medium, High }
static readonly string[] kQualityNames = { "Low", "Medium", "High" };

Ui.dropdown(ref m_quality, "Quality", kQualityNames);
```

Multi selects read and write a `bool[]` you keep around:

```csharp
static readonly string[] kTargetNames = { "Players", "Titans", "Horses" };
static readonly bool[] kTargets = new bool[3];

Ui.multiSelect("Targets", kTargetNames, kTargets);
```

## Showing, hiding and greying out

`reveal` animates rows open and shut. Only draw the rows when it says so.

```csharp
Ui.toggle(ref m_fly, "Fly");

using (Ui.Reveal open = Ui.reveal(m_fly))
{
    if (open)
        Ui.slider(ref m_flySpeed, "Fly Speed", 1f, 50f);
}
```

A reveal is told apart by the line it is on. When the same line runs more than once in a block, like in a loop or a shared helper, pass a key:

```csharp
foreach (Rule rule in m_rules)
{
    using (Ui.Reveal open = Ui.reveal(rule.enabled, rule.id))
    {
        if (open)
            Ui.slider(ref rule.range, "Range", 0f, 100f);
    }
}
```

A disclosure is a header row the user clicks to fold. Keep its open flag in a field:

```csharp
static bool m_advancedOpen;

using (Ui.Reveal open = Ui.disclosure("Advanced", ref m_advancedOpen, "adv_header", "adv_body"))
{
    if (open)
        Ui.toggle(ref m_debug, "Debug Draw");
}
```

`disabled` greys out and locks everything inside:

```csharp
using (Ui.disabled(!m_connected))
    Ui.button("Kick", kick);
```

## Layout

Sub tabs inside one tab:

```csharp
static readonly SubTabHost kPages = SubTabHost.forPages("main_tabs", new[] { ("General", kGeneral), ("Advanced", kAdvanced) });

MenuHost.setTabs(new[] { new TabRow("MAIN", kPages.draw, kPages.drawHeader) });
```

Fixed columns instead of automatic packing:

```csharp
static readonly Page kSkins = Ui.columns(new Action[] { drawList, drawCopy }, new Action[] { drawPreview });
```

`row` reserves space in the block and gives you its `Rect`, `group` brackets drawing you place yourself, and `inset` narrows the rows while something sits beside them:

```csharp
using (Ui.Row bar = Ui.row(6f))
    Gui.drawTexture(bar.rect, Theme.accentBar);

using (Ui.group())
{
    UiText.wrappedLabel(x, y, Ui.width, m_summary, Theme.label, out float height);
    Ui.y += height;
}

using (Ui.inset(kRefreshButtonW))
    Ui.note("No saved configs yet.");
```

## Drawing outside a block

A panel or overlay that owns its own cursor pushes it, draws the same widgets, then pops it:

```csharp
Ui.push(x, y, width);
Ui.toggle(ref m_on, "Enabled");
y = Ui.pop();
```

For a single placed widget, `UiAt` takes the rect instead: `UiAt.toggle`, `UiAt.dropdown`, `UiAt.button`, `UiAt.textField`, `UiAt.infoRow`, `UiAt.emptyState`, `UiAt.progress`, `UiAt.box`, `UiAt.highlight` and the rest.

## Popups and standalone windows

```csharp
Ui.button("Wipe", () => MenuHost.showConfirm("Wipe everything?", wipe));
MenuHost.showInfo("Saved.");
```

`WindowHost` draws a themed window outside the menu, with the menu's input state, so every widget works unchanged. Key it with your own enum:

```csharp
enum MyWindow { Browser }

void onGUI() => WindowHost.onGUI("browser", MyBrowser.open, MyWindow.Browser, "MyBrowser.onGUI", MyBrowser.draw);

static void draw()
{
    Rect window = WindowHost.centered(WindowHost.widthFor(640f), WindowHost.heightFor(480f));
    using (WindowHost.begin("browser", window, "BROWSER", out Rect content))
    {
        Rect inner = WindowHost.inner(content);
        // ...
    }
}
```

## Notifications

Cards reel in from the screen edge, dedupe, and leave again. Register one row per severity you log at, then push:

```csharp
static readonly NotifyLevel[] kLevels = { new NotifyLevel(() => Theme.Status.m_red, () => Theme.horizontalFade(() => Theme.Status.m_red), true), ... };

Host.bind(new Host.Bindings { m_levels = kLevels, m_notifyRef = MyPlayers.tryName });

Notifications.push(level: 0, tag: "WARN", message: "Not in a room");
```

`m_notifyRef` is optional: it turns an `[12]` in a message into a coloured id followed by that player's name.

## Badges

The framework owns the chip, its metrics, the caches and the row draw. The host owns which badges exist:

```csharp
static readonly BadgeRow[] kRows = { new BadgeRow("HOST", () => Theme.m_accent), new BadgeRow("ADMIN", () => Theme.Status.m_red, counts: true) };

Host.bind(new Host.Bindings { m_badges = kRows });

Badges.drawRow(rightX, Badges.rowY(headerRect), m_badgeBuffer);
Badges.drawPopupHeader(popupRect, headerH, pad, accentH, title, Theme.title, m_badgeBuffer);
string tag = Badges.markup((int)MyBadge.Admin, extra: 2);   // "[ADMIN +2]" in the row colour
```

A `counts` row reads "ADMIN +2" when more rows of the same kind apply. Your badge list is any `List<TEnum>`.

## Overlays

Each overlay owns its text style. `OverlayStyle` holds the scale bounds and clamps the overlay's own value:

```csharp
float scale = OverlayStyle.scale(m_scale);

OverlayText.drawTracked(rect, text, style, color, OverlayText.kShadow);
```

`PixelCanvas` is a point-sampled texture you write columns into, for charts. `ShapeRaster` rasterizes a `CrosshairSpec` into an antialiased texture.

## Settings

Mark settings with `[Config(id)]` using any id that stays the same. Trellis never touches the disk; your config system finds the fields, then loads and saves them under that id. Since the id is the saved name, renaming or obfuscating the field keeps the value.

```csharp
[Config(0x5A4C1E2F90B3D771)]
static float m_speed = 1f;
```

If your build stamps ids before obfuscation, write `[Config]` and let the build hash `Owner.Field` into the id. After a rename, pin the old name with `[Config("Owner.oldField")]` so saved values still load.

`sample/` has a complete plugin.
