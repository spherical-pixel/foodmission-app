# 🍎 Foodmission

[![Unity](https://img.shields.io/badge/Unity-6000.3.8f1-black.svg?style=flat-square&logo=unity)](https://unity.com)
[![App UI](https://img.shields.io/badge/App%20UI-2.1.6-blue.svg?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html)
[![License](https://img.shields.io/badge/License-Open%20Source-green.svg?style=flat-square)](LICENSE)

> A gamified mobile application promoting healthy and sustainable eating habits through personalized nutrition tracking, challenges, and community engagement.

---

## 📱 Overview

Foodmission is an open-source Unity application designed to help users develop healthier and more sustainable eating habits. Built with the **Unity App UI** framework following MVVM architecture with Redux state management.

### ✨ Key Features

- 👤 **User Profiles** — Basic and extended profiles with dietary preferences, avatar personalization
- 🛒 **Shopping & Pantry** — Digital inventory with expiry tracking and shopping list management
- 🍽️ **Meal Logging** — Food diary with quick barcode scanning and nutritional analysis
- 📖 **Recipes** — Community-shared recipes with ratings and personal collections
- 🏆 **Challenges & Missions** — Gamified daily challenges and long-term quests with rewards
- 🗑️ **Food Waste Tracking** — Monitor and reduce food waste with carbon footprint insights
- 💡 **Knowledge** — Learning paths on nutrition and sustainability
- 🎮 **Games** — Educational mini-games linked to challenges
- 🌐 **Global Community** — Compare progress with community filters
- 📍 **Sustainable Business Map** — Find eco-friendly food businesses nearby

---

## 🏗️ Architecture

```
FoodmissionAppBuilder (MonoBehaviour)
    ↓ configures
FoodmissionApp : App (Unity App UI entry point)
    ↓ creates
NavHost with Navigation Graph + DI container
```

### Tech Stack

| Component | Technology |
|-----------|------------|
| **Engine** | Unity 6000.3.8f1 (Unity 6) |
| **UI Framework** | Unity App UI v2.1.6 |
| **Architecture** | MVVM with declarative navigation |
| **State Management** | Redux with C# records |
| **Accessibility** | Native Unity 6.0+ APIs |
| **Localization** | Unity Localization |

---

## 🚀 Getting Started

### Prerequisites

- [Unity 6000.3.8f1](https://unity.com/releases/editor/whats-new/6000.3.8) or later
- [Unity Hub](https://unity.com/unity-hub)
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone git@github.com:spherical-pixel/foodmission-app.git
   cd foodmission-app
   ```

2. **Open in Unity Hub**
   - Launch Unity Hub
   - Click **Open** → **Add project from disk**
   - Select the cloned folder

3. **Open the main scene**
   - `Assets/Foodmission/scenes/FoodmissionAppUI.unity`

4. **Run**
   - Press **Play** in Unity Editor, or
   - Build for Android/iOS

---

## 📁 Project Structure

```
Assets/Foodmission/
├── scripts/AppUI/
│   ├── Core/           # AppBuilder, App classes, DI
│   ├── Models/         # State records (AppState, etc.)
│   ├── Services/       # Interfaces and implementations
│   ├── Screens/        # Navigation screens and ViewModels (per-screen folders)
│   ├── Store/          # Redux actions and reducers
│   └── Navigation/     # Generated navigation graph
├── AppUI/              # UXML templates, USS styles
├── scenes/             # Unity scenes
└── docs/               # Documentation and API collections
```

---

## 📚 Documentation

| Resource | Description |
|----------|-------------|
| [PLATFORM_OVERVIEW.md](docs/PLATFORM_OVERVIEW.md) | Feature specifications and data models |
| [docs/app-ui/](docs/app-ui/) | Unity App UI framework documentation (v2.1) |
| [API Docs](https://api.foodmission.eu/api/docs) | Backend API (Swagger UI) |

---

## 🔧 Development

### IDE Setup

Open `Foodmission.slnx` (or `.sln`) in:
- [Visual Studio](https://visualstudio.microsoft.com/) with Unity extension
- [VS Code](https://code.visualstudio.com/) with C# Dev Kit

### Running Tests

1. Open Unity Editor
2. Go to **Window → General → Test Runner**
3. Select PlayMode or EditMode
4. Click **Run All**

### Code Conventions

- **Private fields**: `_camelCase` (e.g., `_storeService`)
- **Public properties**: `PascalCase` (e.g., `LoadingText`)
- **Always use braces** for `if`/`for`/`while` blocks
- Prefer **declarative binding** over manual event subscriptions


---

## 🤝 Contributing

We welcome contributions! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines and code conventions.

---

## 📄 License

This project is open source. See [LICENSE](LICENSE) for details.

---

## 🙏 Acknowledgments

- [Unity App UI](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html) — UI Framework
- [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.5) — i18n support
- Funded by European research programs

---

<p align="center">
  Made with ❤️ for a healthier, more sustainable future
</p>
