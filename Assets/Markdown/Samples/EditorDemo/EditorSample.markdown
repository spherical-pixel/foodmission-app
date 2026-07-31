# Editor Sample

This sample shows how you can use the _Markdown Support_ SDK inside
the editor to extend the tooling and insert markdown where ever 
_UIToolkit_ is supported. 

The `MarkdownSnippetAttribute` and `MarkdownSnippetDrawer` allow
the default Unity inspector to show markdown per field. The `SampleScript`
source code demonstrates how to use it, which I've included below.

```csharp
[MarkdownSnippet(RelativeMarkdownFile = "../MarkdownDocs/EditorSample_fieldDoc.markdown")]
public int x;

[MarkdownSnippet(InlineMarkdown = "---\n A _number_ that will be printed to the console, `" + nameof(x) + "` times")]
public int y;
```

And then, the gist of the special property drawer is that it calls
`UMarkdown.Parse` on its own, like this.

```csharp
UMarkdown.Parse(InlineMarkdown, UMarkdownContext.GetDefault(false))
```