# App UI Documentation v2.1

> **Warning**: App UI is considered as an experimental product. It is provided **"as is"** without warranty of any kind, express or implied.

App UI is a powerful and flexible framework for building beautiful, high-performance user interfaces in Unity. Built on top of Unity UI-Toolkit, it works seamlessly across Android, iOS, Windows, MacOS, and web platforms.

## Table of Contents

### Getting Started

- [Overview](getting-started/index.md) - Introduction to App UI
- [What's New](getting-started/whats-new.md) - Latest changes and updates
- [Migration Guide](getting-started/migration-guide.md) - Migrating from previous versions
- [Requirements](getting-started/requirements.md) - System requirements
- [Installation & Setup](getting-started/installation-setup.md) - Installing and configuring App UI
- [Using App UI](getting-started/using-app-ui.md) - Getting started guide

### References

#### UI Components

- [Actions](references/actions.md) - Buttons and actionable components
- [Inputs](references/inputs.md) - Input fields and controls
- [Input Values](references/input-values.md) - Handling input validation and formatting
- [Layouts](references/layouts.md) - Layout containers and components
- [Typography](references/typography.md) - Text styling and fonts
- [Iconography](references/iconography.md) - Icons and icon usage

#### Core Features

- [Contexts](references/contexts.md) - Context management system
- [Overlays](references/overlays.md) - Popups, modals, and notifications
- [Navigation](references/navigation.md) - Screen navigation system
- [Localization](references/localization.md) - Internationalization support
- [Accessibility](references/accessibility.md) - Accessibility features
- [Native Integration](references/native-integration.md) - Platform-specific features

#### Application Architecture

- [MVVM Introduction](references/architecture/mvvm-intro.md) - Model-View-ViewModel pattern
- [Observables](references/architecture/observables.md) - Data binding with observables
- [Commanding](references/architecture/commanding.md) - Command pattern implementation
- [Dependency Injection](references/architecture/dependency-injection.md) - DI container usage
- [State Management](references/architecture/state-management.md) - Redux-like state management

### Customization

- [Theming](customization/theming.md) - Creating and using themes
- [Styling](customization/styling.md) - CSS styling with USS
- [Custom Icons](customization/custom-icons.md) - Adding custom icons
- [Custom Components](customization/custom-components.md) - Creating custom UI components
- [Custom Typography](customization/custom-typography.md) - Custom fonts and typography

### Samples

- [UI Kit](samples/ui-kit.md) - Component showcase
- [Storybook](samples/storybook.md) - Component testing tool
- [Navigation](samples/navigation.md) - Navigation pattern example
- [MVVM](samples/mvvm.md) - MVVM architecture sample
- [Redux](samples/redux.md) - State management sample
- [MVVM & Redux](samples/mvvm-redux.md) - Combined MVVM + Redux sample
- [Undo/Redo](samples/undo-redo.md) - Undo/redo pattern sample

### Help

- [FAQ](help/faq.md) - Frequently asked questions

## External Resources

- [UI Toolkit Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html)
- [Unity App UI Package (v2.1)](https://docs.unity3d.com/Packages/com.unity.dt.app-ui@2.1/manual/index.html)

## Namespace

All App UI components are in the `Unity.AppUI.UI` namespace.

```csharp
using Unity.AppUI.UI;
using Unity.AppUI.MVVM;
using Unity.AppUI.Navigation;
using Unity.AppUI.Redux;
```

## Quick Start

```xml
<UXML xmlns="UnityEngine.UIElements" xmlns:appui="Unity.AppUI.UI">
    <appui:Panel>
        <appui:Button title="Hello World!" />
    </appui:Panel>
</UXML>
```