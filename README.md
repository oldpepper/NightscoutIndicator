# NightscoutIndicator

[English](#english) | [Русский](#русский) | [简体中文](#简体中文)

## English

A small Windows tray application that always shows your current blood glucose from your personal [Nightscout](https://nightscout.github.io/) server (CGM data from Dexcom, Libre via xDrip/AndroidAPS, etc.).

There is no main window: the app lives in the system tray.

### Features

- **Tray icons** - two modes, selectable in Settings:
  - `TwoIcons`: one icon with the current value and one with the trend arrow on a zone-colored background;
  - `SingleIcon`: one icon with the value on the zone color and a trend stripe.

  The tooltip shows the value; stale data is shown in lilac.
- **Flying indicator** - a semi-transparent square on top of all windows with the trend arrow and the value. Drag it with the left mouse button; its position is saved.
- **Glucose chart** for the last `TimeRange` hours - click the tray icon (left button) or right-click the indicator.
- **Alarms** - a sound and a balloon notification when glucose is above `High` and rising/flat, or below `Low` and falling/flat, not more often than every `AlarmInterval` minutes.
- **Settings window** - double-click the tray icon (or use the context menu). All parameters can be changed and applied without restarting.
- **Localization** - English, Russian and Simplified Chinese; the language is chosen at the top of the Settings window.

### Requirements

- Windows 10 version 1809 (build 17763) or later
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (to run) or .NET 10 SDK (to build)
- A Nightscout server and its API secret

### Build and run

```powershell
git clone https://github.com/oldpepper/NightscoutIndicator.git
cd NightscoutIndicator
dotnet build NightscoutIndicatorSln.sln -c Release
dotnet run --project NightscoutIndicator
```

### Configuration

On first start open **Settings** and enter your Nightscout URL and API secret. Everything else has working defaults.

Settings are layered; each next layer overrides the previous one:

1. `appsettings.json` next to the executable - defaults (the app never writes to it);
2. `%LOCALAPPDATA%\NightscoutIndicator\usersettings.json` - your settings; created on first start and written by the Settings window and by the indicator position saving;
3. `appsettings.Development.json` - developer overrides, used only in Debug builds and ignored by git.

Main parameters (section `AppSettings`):

| Key | Description |
|---|---|
| `NightscoutUrl`, `ApiSecret` | Server address and API secret |
| `UpdateInterval` | Polling interval, seconds |
| `Unit` | `MmolL` or `MgDl` |
| `ExtraLow`, `Low`, `High`, `ExtraHigh` | Glucose zone thresholds |
| `TimeRange` | Chart range, hours |
| `UseAlarm`, `AlarmInterval` | Alarms on/off and minimum interval, minutes |
| `AlarmHiFileName`, `AlarmLowFileName` | Alarm sounds (`.wav`, `.mp3`) |
| `DataExpirationTimeout` | Minutes without new data before the value is considered stale |
| `FlyingIndicatorSize` | Indicator size, pixels |
| `TrendIcon*` | Trend arrow icons for the tray |
| `TrayIconMode` | `TwoIcons` or `SingleIcon` |
| `Language` | Interface language code (`en-US`, `ru-RU`, `zh-CN`) |

> **Note:** the API secret is stored in `usersettings.json` in plain text. Do not commit or share this file.

### Adding a language

Copy `Localization\en-US.json` to `Localization\<culture-code>.json`, keep the keys and translate the values. Set `Language.Name` to the language title shown in the Settings window.

### Third-party components

- [League Gothic](https://github.com/theleagueof/league-gothic) font (`Fonts\`) - SIL Open Font License 1.1, see `NightscoutIndicator\Fonts\LeagueGothic-OFL.md`
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet), [NAudio](https://github.com/naudio/NAudio), [Newtonsoft.Json](https://www.newtonsoft.com/json), Microsoft.Extensions.*, Microsoft.Xaml.Behaviors.Wpf - NuGet packages

## Русский

Небольшое приложение для Windows, которое постоянно показывает в трее текущий уровень глюкозы с вашего личного сервера [Nightscout](https://nightscout.github.io/) (данные НМГ от Dexcom, Libre через xDrip/AndroidAPS и т.п.).

Главного окна нет: приложение работает в системном трее.

### Возможности

- **Иконки в трее** - два режима на выбор в окне настроек:
  - `TwoIcons`: одна иконка с текущим значением и вторая со стрелкой тренда на фоне цвета зоны глюкозы;
  - `SingleIcon`: одна иконка со значением на фоне цвета зоны и полосой тренда.

  В подсказке показывается значение; устаревшие данные выделяются сиреневым цветом.
- **Плавающий индикатор** - полупрозрачный квадрат поверх всех окон со стрелкой тренда и значением. Перетаскивается левой кнопкой мыши, позиция сохраняется.
- **График глюкозы** за последние `TimeRange` часов - по левому клику на иконку в трее или по правому клику на индикатор.
- **Тревоги** - звук и всплывающее уведомление, если глюкоза выше `High` и растёт или стабильна, либо ниже `Low` и падает или стабильна; не чаще раза в `AlarmInterval` минут.
- **Окно настроек** - двойной клик по иконке в трее (или контекстное меню). Все параметры можно менять, они применяются без перезапуска.
- **Локализация** - английский, русский и упрощённый китайский; язык выбирается вверху окна настроек.

### Требования

- Windows 10 версии 1809 (сборка 17763) или новее
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (для запуска) или .NET 10 SDK (для сборки)
- Сервер Nightscout и его API-секрет

### Сборка и запуск

```powershell
git clone https://github.com/oldpepper/NightscoutIndicator.git
cd NightscoutIndicator
dotnet build NightscoutIndicatorSln.sln -c Release
dotnet run --project NightscoutIndicator
```

### Настройка

При первом запуске откройте **Настройки** и укажите адрес сервера Nightscout и API-секрет. Для остальных параметров заданы рабочие значения по умолчанию.

Настройки хранятся в трёх слоях, каждый следующий переопределяет предыдущий:

1. `appsettings.json` рядом с исполняемым файлом - значения по умолчанию (приложение в него не пишет);
2. `%LOCALAPPDATA%\NightscoutIndicator\usersettings.json` - ваши настройки; создаётся при первом запуске, в него пишут окно настроек и сохранение позиции индикатора;
3. `appsettings.Development.json` - настройки разработчика, используются только в Debug-сборке и не попадают в git.

Основные параметры (секция `AppSettings`):

| Ключ | Описание |
|---|---|
| `NightscoutUrl`, `ApiSecret` | Адрес сервера и API-секрет |
| `UpdateInterval` | Интервал опроса, секунды |
| `Unit` | `MmolL` или `MgDl` |
| `ExtraLow`, `Low`, `High`, `ExtraHigh` | Пороги зон глюкозы |
| `TimeRange` | Период графика, часы |
| `UseAlarm`, `AlarmInterval` | Включение тревог и минимальный интервал между ними, минуты |
| `AlarmHiFileName`, `AlarmLowFileName` | Звуки тревоги (`.wav`, `.mp3`) |
| `DataExpirationTimeout` | Через сколько минут без новых данных значение считается устаревшим |
| `FlyingIndicatorSize` | Размер индикатора, пиксели |
| `TrendIcon*` | Иконки стрелок тренда для трея |
| `TrayIconMode` | `TwoIcons` или `SingleIcon` |
| `Language` | Код языка интерфейса (`en-US`, `ru-RU`, `zh-CN`) |

> **Внимание:** API-секрет хранится в `usersettings.json` открытым текстом. Не добавляйте этот файл в репозиторий и не передавайте его другим.

### Добавление языка

Скопируйте `Localization\en-US.json` в `Localization\<код-языка>.json`, сохраните ключи и переведите значения. В `Language.Name` укажите название языка, которое будет показано в окне настроек.

### Сторонние компоненты

- Шрифт [League Gothic](https://github.com/theleagueof/league-gothic) (`Fonts\`) - SIL Open Font License 1.1, см. `NightscoutIndicator\Fonts\LeagueGothic-OFL.md`
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet), [NAudio](https://github.com/naudio/NAudio), [Newtonsoft.Json](https://www.newtonsoft.com/json), Microsoft.Extensions.*, Microsoft.Xaml.Behaviors.Wpf - пакеты NuGet

## 简体中文

NightscoutIndicator 是一款小巧的 Windows 托盘应用程序，可持续显示来自您个人 [Nightscout](https://nightscout.github.io/) 服务器的当前血糖值（CGM 数据，例如 Dexcom，或通过 xDrip/AndroidAPS 接入的 Libre 等）。

程序没有主窗口，常驻在系统托盘中运行。

### 功能

- **托盘图标**：提供两种模式，可在设置窗口中选择：
  - `TwoIcons`：一个图标显示当前血糖值，另一个图标以血糖区间颜色为背景显示趋势箭头；
  - `SingleIcon`：只用一个图标，以血糖区间颜色为背景显示血糖值和趋势条。

  鼠标悬停提示中显示当前血糖值；数据过期时以淡紫色显示。
- **悬浮指示器**：一个半透明方块，始终显示在所有窗口最上层，包含趋势箭头和血糖值。可按住鼠标左键拖动，位置会自动保存。
- **血糖图表**：显示最近 `TimeRange` 小时的数据。左键单击托盘图标或右键单击悬浮指示器即可打开。
- **警报**：当血糖高于 `High` 且趋势为上升或平稳，或低于 `Low` 且趋势为下降或平稳时，播放提示音并显示气泡通知；两次警报之间至少间隔 `AlarmInterval` 分钟。
- **设置窗口**：双击托盘图标（或通过右键菜单）打开。所有参数均可修改，保存后立即生效，无需重启程序。
- **界面语言**：英语、俄语和简体中文；可在设置窗口顶部切换。

### 系统要求

- Windows 10 版本 1809（内部版本 17763）或更高版本
- [.NET 10 桌面运行时](https://dotnet.microsoft.com/download/dotnet/10.0)（用于运行）或 .NET 10 SDK（用于构建）
- 一台 Nightscout 服务器及其 API 密钥

### 构建与运行

```powershell
git clone https://github.com/oldpepper/NightscoutIndicator.git
cd NightscoutIndicator
dotnet build NightscoutIndicatorSln.sln -c Release
dotnet run --project NightscoutIndicator
```

### 配置

首次启动后，请打开**设置**，填写 Nightscout 服务器地址和 API 密钥。其余参数均已预设可直接使用的默认值。

设置分为三层，后一层会覆盖前一层：

1. 可执行文件所在目录中的 `appsettings.json`：默认值（程序不会写入此文件）；
2. `%LOCALAPPDATA%\NightscoutIndicator\usersettings.json`：用户设置；首次启动时自动创建，设置窗口和悬浮指示器的位置保存功能会写入此文件；
3. `appsettings.Development.json`：开发者专用设置，仅在 Debug 构建中生效，不纳入 git 版本管理。

主要参数（`AppSettings` 节）：

| 键 | 说明 |
|---|---|
| `NightscoutUrl`、`ApiSecret` | 服务器地址和 API 密钥 |
| `UpdateInterval` | 数据更新间隔（秒） |
| `Unit` | 血糖单位：`MmolL` 或 `MgDl` |
| `ExtraLow`、`Low`、`High`、`ExtraHigh` | 血糖区间阈值（极低值、低值、高值、极高值） |
| `TimeRange` | 图表时间范围（小时） |
| `UseAlarm`、`AlarmInterval` | 是否启用警报，以及警报最短重复间隔（分钟） |
| `AlarmHiFileName`、`AlarmLowFileName` | 高血糖和低血糖警报声音文件（`.wav`、`.mp3`） |
| `DataExpirationTimeout` | 超过多少分钟未收到新数据即视为数据过期 |
| `FlyingIndicatorSize` | 悬浮指示器大小（像素） |
| `TrendIcon*` | 托盘趋势箭头图标 |
| `TrayIconMode` | 托盘图标模式：`TwoIcons` 或 `SingleIcon` |
| `Language` | 界面语言代码（`en-US`、`ru-RU`、`zh-CN`） |

> **注意：** API 密钥以明文形式保存在 `usersettings.json` 中。请勿将此文件提交到代码仓库或分享给他人。

### 添加新语言

将 `Localization\en-US.json` 复制为 `Localization\<语言代码>.json`，保留所有键不变，只翻译对应的值。将 `Language.Name` 设置为设置窗口中显示的语言名称。

### 第三方组件

- [League Gothic](https://github.com/theleagueof/league-gothic) 字体（`Fonts\`）：采用 SIL Open Font License 1.1 授权，详见 `NightscoutIndicator\Fonts\LeagueGothic-OFL.md`
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet)、[NAudio](https://github.com/naudio/NAudio)、[Newtonsoft.Json](https://www.newtonsoft.com/json)、Microsoft.Extensions.*、Microsoft.Xaml.Behaviors.Wpf：NuGet 包
