# Runtime Sample

In this sample, the `Logic` game object and script applies a 
 `.markdown` asset onto a World Space UI at Runtime. 

The `.markdown` file is a field on the `Logic` game object. By 
default, the `RuntimeSample.markdown` file should
be selected (which is this file). 

The following code is responsible for converting the `markdown` 
asset into a `VisualElement`

```csharp
markdown.BuildRuntimeVisualElement();
```

Then, the `VisualElement` needs to be added into the Runtime `UIDOcument`
by manually injecting it. The sample also injects a `ScrollView`.

Images can be loaded from Resource directories, like this. The url is the
name of the asset in any `./Resources` directory.

![angry](angry_face)

If you need to load an image from assets, then it gets a bit trickier. 
You'll need to provide a custom texture loader on the `UMarkdownContext` instance, like this
```csharp
var context = UMarkdownContext.Runtime();
context.textureLoader = (ctx, uri, applier) =>
{
    // custom logic to load an image.
};
```

And then kerblam, you can load images how ever you want.

![custom](t:house)


Finally, there is a bit of wizardry to make the mouse interactivity
work in world space with UIToolkit. I sourced some code from
a [youtube video](https://www.youtube.com/watch?v=gXx_j-6z8jY) by 
_[Madcat Tutorials](https://www.youtube.com/@madcattutorials2422)_. 

The UI wiggles a bit, but only because this is a demo, and I need it
to look flashy and spiffy. 

