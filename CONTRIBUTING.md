# Contributing to Foodmission

Thank you for your interest in contributing to Foodmission! This document provides guidelines and code conventions to maintain project quality.

## 🚀 Getting Started

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/new-feature`)
3. Commit your changes (`git commit -m 'Add new feature'`)
4. Push to the branch (`git push origin feature/new-feature`)
5. Open a Merge Request

## 📝 Code Conventions

### Naming Conventions

- **Classes**: PascalCase (`SplashScreenViewModel`, `StoreService`)
- **Interfaces**: PascalCase with `I` prefix (`IStoreService`, `IThemeService`)
- **Files**: Match class name exactly
- **Private fields**: `_camelCase` (`_storeService`, `_loadingText`)
- **Public properties**: PascalCase (`LoadingText`, `Theme`, `IsLoading`)
- **Constants**: PascalCase or ALL_CAPS (`APP_SLICE`, `APP_STATE_KEY`)
- **Records**: PascalCase with init-only properties

### Code Block Style

**Always use explicit braces** for `if`, `for`, `foreach`, `while`, `do`, even for single-line blocks:

```csharp
// Correct
if (button != null)
{
    button.clicked += OnButtonClicked;
}

// Incorrect
if (button != null)
    button.clicked += OnButtonClicked;
```

### Using Statement Order

```csharp
using System;
using System.Collections.Generic;

using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Redux;

using UnityEngine;
using UnityEngine.UIElements;
```

## 🏗️ Architecture

### MVVM Pattern

ViewModels inherit from `ViewModelBase`:

```csharp
[ObservableObject]
public partial class MyViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _loadingText;

    public MyViewModel(IStoreService storeService) : base(storeService)
    {
        // Automatic subscriptions
    }
}
```

**ObservableProperty Convention:**
The `[ObservableProperty]` generator creates a **public property** from the private field:
- `_loadingText` → `LoadingText` (public property)
- `_isLoading` → `IsLoading` (public property)

### Data Binding

**Prefer declarative binding** over manual subscriptions:

```xml
<!-- In UXML -->
<appui:Text binding-path="LoadingText" />
<appui:Toggle binding-path="IsEnabled" />
```

```csharp
// In Screen (automatic via NavigationScreenBase)
contentContainer.dataSource = _viewModel;  // Enables all bindings
```

### Navigation Screens

Screens inherit from `NavigationScreenBase<TViewModel>`:

```csharp
[Preserve]
class HomeScreen : NavigationScreenBase<HomeScreenViewModel>
{
    public HomeScreen()
    {
        InitializeComponent(FoodmissionAppBuilder.instance.homeTemplate);
    }

    protected override void OnViewModelBound()
    {
        base.OnViewModelBound();
        // Manual setup here
    }
}
```

### Dependency Injection

Register services in `FoodmissionAppBuilder.OnConfiguringApp`:

```csharp
// Singletons
builder.services.AddSingleton<ILocalStorageService, LocalStorageService>();
builder.services.AddSingleton<IStoreService, StoreService>();

// Transient (new instance per resolution)
builder.services.AddTransient<SplashScreenViewModel>();
```

## 🎨 Styling & Theming

### CSS Variables

**App UI built-in** (use for consistency):
- `--appui-font-sizes-body-md`, `--appui-font-sizes-body-lg` (14px, 16px, scaled)
- `--appui-primary-15` (black), `--appui-primary-1300` (white)
- `--appui-foregrounds-100` (primary text color, theme-aware)
- `--appui-spacing-*`, `--appui-sizing-*`

**Custom variables** (define in `Foodmission_Theme.uss`):
```css
.appui--light {
    --fm-main-bg: #FAFAFA;
    --fm-text-primary: #1A1A1A;
}

.appui--dark {
    --fm-main-bg: #121212;
    --fm-text-primary: #FFFFFF;
}
```

## ♿ Accessibility

The project uses **Unity 6.0+ native accessibility APIs**:
By now it's not being implemented but it will.


## 🧪 Testing

Tests are in `Assets/Foodmission/Tests/Editor/` using Unity Test Framework:

1. Open Unity Editor
2. Go to **Window → General → Test Runner**
3. Select PlayMode or EditMode
4. Click **Run All**

## 📚 Resources

- [Unity App UI Documentation](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html)
- [Platform Overview](docs/PLATFORM_OVERVIEW.md)
- [API Docs](https://api.foodmission.eu/api/docs)

## ❓ Questions

If you have questions, open an issue or contact the development team.
duran@devilishgames.com

---

Thank you for contributing to a healthier, more sustainable future! 🌱
