# Layouts

App UI provides a variety of layout components to help you create the UI of your Unity project.

## Layout Components

### App UI Panel

The [App UI Panel](../api/Unity.AppUI.UI.Panel.html) component is the root component of the App UI system. At the layout level, it provides a layering system to handle popups, notifications, and tooltips. They will be displayed as overlays in the same [UIDocument](https://docs.unity3d.com/ScriptReference/UIElements.UIDocument.html).

### Containers

Containers are layout components that contain several similar child elements. They are used to group elements together and to apply a layout to them.

#### StackView

The [StackView](../api/Unity.AppUI.UI.StackView.html) component is a container that arranges its children one over the other, in a Z-axis stack. You can push and pop elements from the stack at runtime, and you can also animate the transition between the elements.

![StackView](images/stackview.gif)

#### SwipeView

The [SwipeView](../api/Unity.AppUI.UI.SwipeView.html) component is a container that arranges its children in a horizontal or vertical stack, and allows the user to swipe between them. This view can display more than one child at a time, and it can also be used to create a *carousel* effect by using the wrap mode.

![SwipeView](images/swipeview.gif)

![SwipeView](images/swipeview-2.gif)

#### PageView

The [PageView](../api/Unity.AppUI.UI.PageView.html) component is a the composition of a [SwipeView](../api/Unity.AppUI.UI.SwipeView.html) and a [PageIndicator](../api/Unity.AppUI.UI.PageIndicator.html) components. It allows the user to swipe between pages, and to see the current page index.

### Dialogs

Dialogs are layout components that are used to display a message to the user.

#### Dialog

The [Dialog](../api/Unity.AppUI.UI.Dialog.html) component is a layout component composed of a heading, a body, and a footer.

#### Alert

The [AlertDialog](../api/Unity.AppUI.UI.AlertDialog.html) component is similar to the [Dialog](#dialog) component, but its styling will depend on its [AlertSemantic](../api/Unity.AppUI.UI.AlertSemantic.html) property.

### Others Views

#### SplitView

The [SplitView](../api/Unity.AppUI.UI.SplitView.html) component is a layout component that allows the user to resize its children.

#### VisualElement & ScrollView

The [VisualElement](https://docs.unity3d.com/ScriptReference/UIElements.VisualElement.html) and [ScrollView](https://docs.unity3d.com/ScriptReference/UIElements.ScrollView.html) components are layout components that are part of the [UI Toolkit](https://docs.unity3d.com/6000.0/Documentation/Manual/UIElements.html) system. When you didn't find a layout component that fits your needs, you can use these components to create your own layout.