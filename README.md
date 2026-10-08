# 🍎 Foodmission

[![Unity](https://img.shields.io/badge/Unity-6000.3.12f1-black.svg?style=flat-square&logo=unity)](https://unity.com)
[![App UI](https://img.shields.io/badge/App%20UI-2.1.12-blue.svg?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html)
[![EU Horizon](https://img.shields.io/badge/EU%20Horizon-101181774-blue.svg?style=flat-square)](https://www.foodmission.eu/)

> A gamified citizen science mobile platform promoting healthy and sustainable eating habits, developed as part of the EU Horizon project FOODMISSION (grant 101181774).

---

## 📱 Overview

Foodmission is a Unity application designed to help users develop healthier and more sustainable eating habits. Built with the **Unity App UI** framework following MVVM architecture with Redux state management.

### ✨ Key Features

- 🤖 **Nutri** — Mascot that guides the user, with a unified daily check-in
- 👤 **User Profiles** — Onboarding survey, goals, dietary preferences and avatar personalization
- 🏆 **Quests & Missions** — Long-term quests and daily missions, with per-dimension levels and badges
- 🍽️ **Meal Logging** — Quick and detailed food diary with barcode scanning (Open Food Facts) and a review step
- 🛒 **Shopping & Pantry** — Shopping lists and pantry inventory with expiry tracking
- 🗑️ **Food Waste Tracking** — Log and reduce food waste
- 📖 **Recipes** — Recipe book, detail view and recipe editor
- 💡 **Knowledge** — Quizzes and food facts on nutrition and sustainability
- ⚖️ **Food Comparison** — Compare products side by side
- 👥 **Groups** — Join groups and see members
- 📋 **Pilot Surveys** — In-app surveys for the citizen-science pilots
- 🔔 **Notifications** — Local notifications and an in-app notification list

### 🗺️ Planned

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
| **Engine** | Unity 6000.3.12f1 (Unity 6) |
| **UI Framework** | Unity App UI v2.1.12 |
| **Architecture** | MVVM with declarative navigation |
| **State Management** | Redux with class-based state + `Copy()` pattern |
| **Accessibility** | Native Unity 6.0+ APIs |
| **Localization** | Unity Localization (with remote overrides) |
| **Asset loading** | Addressables (UXML templates via `TemplateService`) |
| **Networking** | `UnityWebRequest` + Newtonsoft JSON, Open Food Facts for product data |
| **Rendering** | URP (Nutri mascot and avatar) |
| **Notifications** | Unity Mobile Notifications |

---

## 🚀 Getting Started

### Prerequisites

- [Unity 6000.3.12f1](https://unity.com/releases/editor/whats-new/6000.3.12) or later
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

### API Environment

The backend URLs live in `Assets/Foodmission/Resources/ApiEnvironmentConfig.asset`, which defines three environments:

| Environment | API |
|-------------|-----|
| Staging | `https://staging.api.foodmission.eu` |
| Test | `https://test.api.foodmission.eu` |
| Local | `http://localhost:3000` |

Select the active one in that asset. Code must always read the URL from `ApiConfig.BaseUrl`, never hardcode it.

---

## 📁 Project Structure

```
Assets/Foodmission/
├── scripts/AppUI/
│   ├── Core/           # AppBuilder, App classes, DI
│   ├── Components/     # Reusable UI components (FMButton, FMDialog, FMItem*…)
│   ├── Models/         # State and API models
│   ├── Services/       # Interfaces and implementations
│   ├── Screens/        # Screens, ViewModels, UXML and USS (per-screen folders)
│   ├── Store/          # Redux actions and reducers
│   └── Navigation/     # Generated navigation graph
├── AppUI/              # Shared UXML templates, theme and USS styles
├── Resources/          # ApiEnvironmentConfig, fonts
├── localization/       # String tables
├── Editor/             # Editor tooling
├── scenes/             # Unity scenes
└── Tests/Editor/       # Unity Test Framework (EditMode)
```

---

## 📚 Documentation

| Resource | Description |
|----------|-------------|
| [App UI Docs](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html) | Unity App UI framework documentation (v2.1) |
| [API Docs](https://staging.api.foodmission.eu/api/docs) | Backend API — staging environment (Swagger UI) |
| [CHANGELOG.md](CHANGELOG.md) | Release history |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Contribution guidelines and code conventions |

---

## 🔧 Development

### IDE Setup

Open `Foodmission.slnx` in:
- [Visual Studio](https://visualstudio.microsoft.com/) with Unity extension
- [VS Code](https://code.visualstudio.com/) with C# Dev Kit

### Running Tests

1. Open Unity Editor
2. Go to **Window → General → Test Runner**
3. Select the **EditMode** tab (all tests live in `Foodmission.Tests.Editor`)
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

License to be defined.

---

## 🙏 Acknowledgments

- [Unity App UI](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html) — UI Framework
- [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.5) — i18n support
- Funded by the European Union — Horizon Europe programme, grant agreement 101181774 (HORIZON-CL6-2024-FARM2FORK-01-6)

---

<p align="center">
  Made with ❤️ for a healthier, more sustainable future
</p>
