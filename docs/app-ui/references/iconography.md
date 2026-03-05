# Iconography

The Iconography of App UI regroups more than 100 icons in total, but for the sake of optimization, we only provide a subset of them in the default [themes](../customization/theming.md). The icons are provided as PNG files and are set up to be loaded via USS classes.

If you are willing to expand the set of icons, you can add your own icons to the project and reference them in a USS file. We also provide an Editor tool called [Icon Browser](../customization/custom-icons.md#icon-browser) to generate the USS classes for you.

For more information about Icon customization, see [Custom Icons](../customization/custom-icons.md).

## Usage

The [Icon](../api/Unity.AppUI.UI.Icon.html) UI component is used to display an icon. The Icon UI component uses these USS classes to load the appropriate icon.

```xml
<Icon name="icon-name" />
```

Note that by default, all icons are white. To change the color of an icon, you can use the `--unity-image-tint-color` custom USS property. For example, to tint the add icon blue, you would use the following code:

```css
.icon-blue {
    --unity-image-tint-color: blue;
}
```

## About Vector Graphics

We are working on integrating Vector Graphics support for icons in a future release. This will allow for more customizable features, such as custom thickness and multi-color support.

Stay tuned for updates on this feature!