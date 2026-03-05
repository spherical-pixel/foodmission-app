# Inputs

Inputs components are used to get input from the user through UI controls. App UI provides a wide range of Input components that can be easily integrated into your Unity projects.

## Input Label

The [InputLabel](../api/Unity.AppUI.UI.InputLabel.html) element is used to display a name next to an input control. The InputLabel element can be used with any input control and will decorate it with a label, required indicator and helper text.

```csharp
// This code creates a Name field with a label.
// The field is valid if it contains at least 1 character.
var myField = new TextField();
var label = new InputLabel("Name");
label.inputAlignment = Align.Stretch;
label.labelOverflow = TextOverflow.Ellipsis;
label.indicatorType = IndicatorType.Asterisk;
label.required = true;
label.helpVariant = HelpTextVariant.Destructive;
myField.validateValue = val => !string.IsNullOrEmpty(val);
myField.RegisterValueChangedCallback(evt =>
{
    label.helpMessage = myField.invalid ? "Name is required" : null;
});
```

## Boolean Inputs

Boolean inputs are used to get a boolean value from the user, typically through a checkbox or a toggle.

![Toggle](images/toggle.png)

In App UI, you can use the [Checkbox](../api/Unity.AppUI.UI.Checkbox.html) element to create a checkbox, and the [Toggle](../api/Unity.AppUI.UI.Toggle.html) element to create a toggle.

## Selection Inputs

Selection inputs are used to get a value from a list of pre-defined values.

![Dropdown](images/dropdown.png)

In App UI, you can use the [Dropdown](../api/Unity.AppUI.UI.Dropdown.html) element to create a dropdown list, and the [RadioGroup](../api/Unity.AppUI.UI.RadioGroup.html) element to create a radio button group.

## Color Inputs

Color inputs are used for selecting colors.

![Color Picker](images/color-picker.png)

## Text Inputs

Text inputs are used for entering text values.

![Text Field](images/text-field.png)

In App UI, you can use the [TextField](../api/Unity.AppUI.UI.TextField.html) element to create a text field, and the [TextArea](../api/Unity.AppUI.UI.TextArea.html) element to create a text area.

## Numeric Inputs

Although there are multiple data structure that implies numerical values. Also, the precision of the value can vary from one data structure to another. We aim to provide different version of our component in order to fit the needs of the user.

### Sliders

Slider inputs are used for selecting a value from a range of values.

![Slider](images/slider.png)

In App UI, you can use the [SliderFloat](../api/Unity.AppUI.UI.SliderFloat.html) element to create a slider. You can also use the [SliderInt](../api/Unity.AppUI.UI.SliderInt.html) element to create a slider with integer values. For touch devices, you can use the [TouchSliderFloat](../api/Unity.AppUI.UI.TouchSliderFloat.html) element to create a slider. You can also use the [TouchSliderInt](../api/Unity.AppUI.UI.TouchSliderInt.html) element to create a slider with integer values.

### Numerical Fields

Numeric inputs are used for entering numerical values.

![Numeric Field](images/numeric-field.png)

### Vectors

![Vector Field](images/vector-field.png)

### Rects and Bounds

![Rect Field](images/rect-field.png)

![Bounds Field](images/bounds-field.png)

### Expression Evaluation

![Expression Evaluator](images/expression-evaluator.gif)