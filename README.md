# Riomaster Analytics — Unity SDK

Game analytics SDK for [Riomaster Analytics](https://github.com/Happy-Hour-Games/Riomaster-Analytics) service.

## Installation

### Option 1: Unity Package Manager (Recommended)

1. Open **Window > Package Manager**
2. Click **+ > Add package from git URL**
3. Enter: `https://github.com/Happy-Hour-Games/Riomaster-Analytics-Unity-SDK.git`

### Option 2: .unitypackage

Download the latest `.unitypackage` from [Releases](https://github.com/Happy-Hour-Games/Riomaster-Analytics-Unity-SDK/releases).

## Quick Start

### 1. Create Config

Go to **Riomaster > Analytics Dashboard** in the Unity menu, then click **Create Analytics Config Asset**. Fill in your server URL and API key.

### 2. Add to Scene

Click **Create Analytics GameObject in Scene**, then drag your config asset to the `Config` field.

### 3. Track Events
```csharp
using Riomaster.Analytics;

public class GameManager : MonoBehaviour
{
    void Start()
    {
        // Set player ID (e.g., Steam ID)
        RiomasterAnalytics.Instance.SetPlayerId("player_123");

        // Track game start
        RiomasterAnalytics.Instance.TrackSessionStart();
    }

    void OnLevelComplete()
    {
        RiomasterAnalytics.Instance.TrackLevelComplete("level_1", 45.2f);
        RiomasterAnalytics.Instance.TrackCurrencyEarned("gold", 100, "level_reward");
    }
}
```

## API Reference

### Core

| Method | Description |
|--------|-------------|
| `Initialize(url, apiKey)` | Initialize with server URL and API key |
| `SetPlayerId(id)` | Set current player identifier |
| `Track(eventName)` | Track a simple event |
| `TrackValue(eventName, float)` | Track with numeric value |
| `TrackValue(eventName, string)` | Track with string value |
| `Track(eventName, properties)` | Track with custom properties |
| `Flush()` | Manually send queued events |

### Session

| Method | Description |
|--------|-------------|
| `TrackSessionStart()` | Track session start |
| `TrackSessionEnd(duration)` | Track session end |
| `NewSession()` | Start a new session |

### Progression

| Method | Description |
|--------|-------------|
| `TrackLevelStart(name)` | Player started a level |
| `TrackLevelComplete(name, time)` | Player completed a level |
| `TrackLevelFail(name, time)` | Player failed a level |

### Economy

| Method | Description |
|--------|-------------|
| `TrackCurrencyEarned(currency, amount, source)` | Currency earned |
| `TrackCurrencySpent(currency, amount, item)` | Currency spent |
| `TrackItemAcquired(itemId, type, source)` | Item acquired |

### Error

| Method | Description |
|--------|-------------|
| `TrackError(type, message)` | Track a custom error |
| `TrackException(exception)` | Track a C# exception |

## Requirements

- Unity 2021.3 or later
- Riomaster Analytics server running

## License

MIT
```

---

## Dosya 10: `LICENSE`
```
MIT License

Copyright (c) 2026 Happy Hour Games

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.