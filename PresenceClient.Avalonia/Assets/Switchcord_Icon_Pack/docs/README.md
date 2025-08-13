# Switchcord Icon Pack — Taskbar + Tray (Nintendo Switch × Discord)

These assets are stylized, original artwork inspired by the Nintendo Switch and Discord marks. They are **not official logos**. 
Please follow the brand guidelines of Nintendo and Discord if you intend to use official trademarks.

## What’s inside

- **windows/**
  - `taskbar.ico` — multires (16,20,24,32,48,64,128,256)
  - `tray.ico` — multires (16,20,22,24,32)
  - `png/` — PNGs in common sizes for both taskbar and tray (light/dark variants for tray)

- **macos/**
  - `SwitchcordAppIcon.appiconset/` — drop this folder into an Xcode asset catalog, or run:
    ```bash
    iconutil -c icns SwitchcordAppIcon.appiconset
    ```
    to produce an `SwitchcordAppIcon.icns` file for non-Xcode apps.
  - `tray/`
    - `SwitchcordStatusTemplateLight@1x.png`, `SwitchcordStatusTemplateLight@2x.png`
    - `SwitchcordStatusTemplateDark@1x.png`, `SwitchcordStatusTemplateDark@2x.png`
    These are monochrome PNGs intended to be used as **template images** for the status bar.
    In AppKit, set `isTemplate = true` on the NSImage. In SwiftUI, prefer symbol rendering or template rendering mode.

- **linux/**
  - `taskbar.svg` + PNG sizes (in linux/taskbar)
  - `tray-light.svg`, `tray-dark.svg` + PNG sizes (in linux/tray)

## Usage quick-start

### Windows (Win32/WPF/UWP/Qt)
- **Taskbar**: set `taskbar.ico` as the app icon/resource.
- **Tray**: assign `tray.ico` to the `NotifyIcon` or equivalent. Prefer the light version for dark system trays; dark version for light trays.

### macOS (AppKit/SwiftUI)
- **Taskbar**: add `SwitchcordAppIcon.appiconset` to your Asset Catalog (or convert to `.icns` via `iconutil` and set it in `Info.plist` as `CFBundleIconFile`).
- **Tray**: use the `SwitchcordStatusTemplateLight/Dark` PNGs and mark them as template. macOS auto-tints template images to match the menu bar.

### Linux (GTK/Qt/Electron)
- **Taskbar**: use `taskbar.svg` or a PNG size appropriate to the environment.
- **Tray**: use `tray-light.svg` or `tray-dark.svg` depending on theme, or detect theme and swap dynamically.

## Legal
Nintendo Switch™ and Discord® are trademarks of their respective owners. These icons are provided "as is" without any warranty. 
Ensure your usage complies with local laws and each brand’s guidelines.
