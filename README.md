# Trellis

IMGUI menu framework for Unity IL2CPP games modded with BepInEx 6 and Il2CppInterop. Themed window with tabs, widgets, popups, notifications and overlays.

## Setup

Compile the source into your plugin:

```xml
<ItemGroup>
  <Using Include="Trellis" />
  <Compile Include="../trellis/source/**/*.cs" Link="trellis/%(RecursiveDir)%(Filename)%(Extension)" />
</ItemGroup>
```

Bind the host once. Only the log sinks are required.

```csharp
Host.bind(new Host.Bindings
{
    m_debug = log.LogDebug,
    m_info = log.LogInfo,
    m_warn = log.LogWarning,
    m_trace = log.LogDebug,
    m_exception = (where, ex) => log.LogError($"{where}: {ex}")
});
```

Drive it from a MonoBehaviour:

```csharp
void Update()
{
    if (Input.GetKeyDown(KeyCode.F1))
        MenuHost.toggle();

    MenuHost.update(true);
}

void OnGUI()
{
    MenuHost.cacheInput();
    MenuHost.onGUI();
    Notifications.onGUI();
}
```

## A menu

```csharp
static bool m_enabled = true;
static float m_speed = 1f;

static readonly Page kMain = Ui.page(drawGeneral);

static void install()
{
    MenuHost.setTitle("My Mod", "v1.0");
    MenuHost.setTabs(new[] { new TabRow("MAIN", kMain.draw) });
}

static void drawGeneral()
{
    Ui.header("General");
    Ui.toggle(ref m_enabled, "Enabled");
    Ui.slider(ref m_speed, "Speed", 0f, 4f);
    Ui.button("Reset", reset);
}
```

Widgets take a field by `ref` and return true when it changes.

## Showing rows conditionally

```csharp
using (Ui.Reveal open = Ui.reveal(m_enabled))
{
    if (open)
        Ui.slider(ref m_speed, "Speed", 0f, 4f);
}
```

## Popups and notifications

```csharp
MenuHost.showConfirm("Reset everything?", reset);
Notifications.push(level: 0, tag: "INFO", message: "Saved");
```

See `sample/` for a complete plugin.
