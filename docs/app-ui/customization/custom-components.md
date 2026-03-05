# Custom UI Components

Every UI component provided by App UI use UI Toolkit and USS to create their visual appearance.

To create completely new UI components, you can use the same tools that App UI uses.

Here are some links that can help you get started:

- [UI Toolkit Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html)
- [UXML Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-UXML.html)
- [USS Documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/UIE-USS.html)

## Useful App UI classes for your components

### KeyboardFocusController

The [KeyboardFocusController](../api/Unity.AppUI.UI.KeyboardFocusController.html) class is used to manage keyboard focus in App UI. With this manipulator you will be able to differentiate a focus triggered by a mouse click from a focus triggered by a keyboard event.

```csharp
public class MyCustomControl : VisualElement
{
    public MyCustomControl()
    {
        this.AddManipulator(new KeyboardFocusController(
            OnKeyboardFocus,
            OnPointerFocus,
        ));
    }
}
```

### Pressable

The [Pressable](../api/Unity.AppUI.UI.Pressable.html) class is used to manage the pressing of an [actionable](../references/actions.md) element.

You should also provide a way to access this manipulator outside of your control, so you can handle the press event anywhere in your app.

```csharp
public class MyCustomControl : VisualElement
{
    readonly Pressable m_Pressable;

    public MyCustomControl()
    {
        m_Pressable = new Pressable(OnPressedInternal);
    }

    private void OnPressedInternal()
    {
        // Handle the press event here
    }
}
```

### IDismissInvocator

The [IDismissInvocator](../api/Unity.AppUI.UI.IDismissInvocator.html) interface is used on [VisualElement](https://docs.unity3d.com/ScriptReference/UIElements.VisualElement.html) that serves as content for a [Popup](../api/Unity.AppUI.UI.Popup.html).

Implementing this interface allows you to dismiss the popup from the content of the popup itself.

> **Important**: The popup content itself must implement this interface. The content of the popup is the element you pass when building the popup.

```csharp
Modal modalPopup = Modal.Build(myRootPanel, myContent);
```

```csharp
public class MyCustomControl : VisualElement, IDismissInvocator
{
    // Implement the IDismissInvocator interface
    public event Action<DismissType> dismissRequested;

    // ...

    // Example with the handling of a "Cancel" button press
    public void OnCancelButtonPressed()
    {
        // Dismiss the popup and give the reason for the dismissal
        dismissRequested?.Invoke(DismissType.Manual);
    }
}
```

### BaseVisualElement and BaseTextElement

The [BaseVisualElement](../api/Unity.AppUI.UI.BaseVisualElement.html) and [BaseTextElement](../api/Unity.AppUI.UI.BaseTextElement.html) classes are the base classes for all App UI components. They provide a set of common optional properties to override the context of the visual tree (such as the theme, the style, the layout direction, etc.).

We recommend that you derive your custom controls from these classes to benefit from the common properties or if you need to deal with context provisioning in general.

```csharp
// for non text-based controls
public class MyCustomControl : BaseVisualElement
{
   // ...
}

// for text-based controls
public class MyCustomTextControl : BaseTextElement
{
   // ...
}
```

### ExVisualElement

The [ExVisualElement](../api/Unity.AppUI.UI.ExVisualElement.html) class is derived from [BaseVisualElement](../api/Unity.AppUI.UI.BaseVisualElement.html) and provides more styling options, such as the support of `box-shadow` and `outline`.

```csharp
public class MyCustomControl : ExVisualElement
{
    public MyCustomControl()
    {
        // Set the pass mask to only render specific passes
        passMask = Passes.Clear
            | Passes.Borders
            | Passes.BackgroundColor
            | Passes.OutsetShadows;
        // Define a USS class to customize the styling
        AddToClassList("my-custom-control");
    }
}
```

```css
.my-custom-control {
    // ...
    --box-shadow-offset-x: 0;
    --box-shadow-offset-y: 8;
    --box-shadow-spread: 16;
    --box-shadow-blur: 15;
    --box-shadow-color: rgba(0,0,0,.65);
}
```

### LocalizedTextElement

If you work with the [Unity Localization](https://docs.unity3d.com/Packages/com.unity.localization@1.4/manual/index.html) package, you can use the [LocalizedTextElement](../api/Unity.AppUI.UI.LocalizedTextElement.html) class to display localized text in your UI.

More information about localization can be found in the [Localization](../references/localization.md) section.

```csharp
public class MyCustomControl : VisualElement
{
    public MyCustomControl()
    {
        // Create a new LocalizedTextElement
        var localizedText = new LocalizedTextElement("@tableName:entryKey");
        // Add it to the hierarchy
        Add(localizedText);
    }
}
```