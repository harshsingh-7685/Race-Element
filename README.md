# Race Element – EA SPORTS WRC Fork

> [!IMPORTANT]
> This repository is a personal, unofficial fork of [Race Element](https://github.com/RiddleTime/Race-Element). Its `ea-wrc` branch adds EA SPORTS WRC support. It is not affiliated with or endorsed by the Race Element developers, Electronic Arts, Codemasters, or WRC.
>
> The EA SPORTS WRC implementation in this fork was created entirely using AI coding tools. I did not personally write the implementation code. My role was to direct the implementation, provide and test telemetry, test builds in-game, report behavior and failures, refine the requirements, and validate the resulting functionality. This disclosure applies only to the EA SPORTS WRC additions in this fork; it does not describe the original Race Element project or the work of its upstream authors and contributors.
>
> This fork is a working snapshot, not an official Race Element release or a replacement for the upstream project. It may be updated again, but it is not guaranteed to remain synchronized with future upstream releases. For the normal and current Race Element project, use the [upstream repository](https://github.com/RiddleTime/Race-Element).

**EA WRC snapshot:** Race Element `2.7.1.7`, based on upstream `dev` commit [`55121bbaf163768813ffc1dc7d622c2b4056be8e`](https://github.com/RiddleTime/Race-Element/commit/55121bbaf163768813ffc1dc7d622c2b4056be8e), updated September 23, 2026.

The original Race Element code and documentation remain credited to their upstream authors and contributors. This fork retains the repository's [GNU General Public License v3](LICENSE.txt).

## EA SPORTS WRC Support

The `ea-wrc` branch adds automatic EA SPORTS WRC process detection and a UDP telemetry provider. It receives the game's `session_update` packets on `127.0.0.1:20877` at 60 Hz using the included [`race_element.json`](https://github.com/harshsingh-7685/Race-Element-EA-WRC/blob/ea-wrc/Race%20Element.Data/Games/EASportsWRC/Diagnostics/race_element.json) packet structure.

The provider currently maps throttle, brake, handbrake, steering, vehicle speed, and four-wheel contact-patch data into Race Element. It calculates direction-independent longitudinal wheel slip for forward and reverse driving, suppresses unstable low-speed slip below 1 m/s, and clears stale control and slip data after 500 ms without a valid packet.

EA SPORTS WRC is also enabled for Race Element's existing DSX overlay. When that overlay and DSX are configured, the mapped brake/throttle inputs and wheel-slip values feed the existing generic DualSense adaptive-trigger feedback behavior.

### EA WRC telemetry setup

EA WRC does not send this custom packet by default, and this fork does not edit the game's configuration automatically. Configure it manually while the game is closed:

1. Locate the EA WRC telemetry directory, normally `Documents\My Games\WRC\telemetry` (it may be under OneDrive when the Windows Documents folder is redirected).
2. Copy the repository's `Race Element.Data\Games\EASportsWRC\Diagnostics\race_element.json` file to `telemetry\udp\race_element.json`.
3. Add the following packet assignment to the existing `udp.packets` array in `telemetry\config.json`, preserving the rest of that file:

```json
{
  "structure": "race_element",
  "packet": "session_update",
  "ip": "127.0.0.1",
  "port": 20877,
  "frequencyHz": 60,
  "bEnabled": true
}
```

### Current scope and limitations

- The EA SPORTS WRC provider supplies the local controls, speed, and longitudinal slip data needed for the current DSX behavior; it is not a complete mapping of every EA WRC telemetry channel into Race Element.
- Telemetry must use the included `race_element` packet structure, `session_update`, loopback address, and UDP port shown above.
- Diagnostic CSV logging remains available only when Race Element is launched with `/EAWrcDiagnostic`; it is not enabled during normal use.
- This custom build stores its application state separately under `%APPDATA%\Race Element EA WRC\`.
- The official Race Element self-updater is disabled in this fork so that an upstream executable cannot overwrite the custom build. Fork and upstream updates must currently be handled manually.

---

## Original Race Element README

[![Downloads](https://img.shields.io/github/downloads/riddletime/race-element/RaceElement.exe?style=flat&label=Downloads&color=%23FF4500
)](https://race.elementfuture.com/guide/how-to-get-started)
[![Discord](https://badgen.net/discord/members/26AAEW5mUq?icon=discord&color=5562ea&label=Race%20Element)](https://discord.gg/26AAEW5mUq)

![Race Element - Name](https://user-images.githubusercontent.com/4581237/209894151-3f8a5dc5-45de-4d7c-a46a-8c5a2a57cd91.png)
# Solutions for Driving and Flight Simulators
- HUDs
- Telemetry
- Setups
- Liveries
- Racing Tools

[Download Latest Release](https://github.com/RiddleTime/Race-Element/releases/latest)


## App Requires .NET 10 Desktop Runtime
https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.0-windows-x64-installer

### App
- WPF
- [MaterialDesign In Xaml Toolkit](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [LiteDB](https://github.com/mbdavid/LiteDB)
- [ScottPlot](https://github.com/scottplot/scottplot)
### Website
- [Angular](https://github.com/angular/angular)
- [Analog](https://github.com/analogjs/analog)

## Code Signing Policy
Free code signing provided by [SignPath.io](https://signpath.io?utm_source=foundation&utm_medium=github&utm_campaign=race-element), certificate by [SignPath Foundation](https://signpath.org?utm_source=foundation&utm_medium=github&utm_campaign=race-element)

## Contributors
<!-- START_CONTRIBUTORS -->
- [RiddleTime](https://github.com/RiddleTime)
- [Reinier-Klarenberg](https://github.com/Reinier-Klarenberg)
- [iFuSiiOnzZ](https://github.com/iFuSiiOnzZ)
- [KrisV147](https://github.com/KrisV147)
- [floriwan](https://github.com/floriwan)
- [ConnorMolz](https://github.com/ConnorMolz)
- [Andrei-Jianu](https://github.com/Andrei-Jianu)
- [Dirk](https://github.com/Dirk)
- [Florian](https://github.com/Florian)
- [goeflo](https://github.com/goeflo)
- [GitHub-Action](https://github.com/GitHub-Action)
- [Andi-Maier](https://github.com/Andi-Maier)
- [dirkaw](https://github.com/dirkaw)
- [Mominon](https://github.com/Mominon)
- [RST](https://github.com/RST)
- [Marco-De-Fanti](https://github.com/Marco-De-Fanti)
- [Glen-Germaine](https://github.com/Glen-Germaine)
- [Connor-Molz](https://github.com/Connor-Molz)
- [Balzs-Fehr](https://github.com/Balzs-Fehr)
- [Aaron-Fang](https://github.com/Aaron-Fang)
- [mreininger23](https://github.com/mreininger23)
- [aaronfang](https://github.com/aaronfang)
- [Kris](https://github.com/Kris)
- [Hans-Brody](https://github.com/Hans-Brody)
- [Dirk-W](https://github.com/Dirk-W)
- [CrayzyCray](https://github.com/CrayzyCray)
- [CannaMan](https://github.com/CannaMan)
- [Andreas-Willich](https://github.com/Andreas-Willich)
<!-- END_CONTRIBUTORS -->
