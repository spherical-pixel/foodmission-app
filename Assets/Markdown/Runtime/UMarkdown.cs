using Markdig;

namespace BrewedInk.MarkdownSupport
{
    /// <summary>
    /// The primary class for the Markdown Support tool.
    /// Use the <see cref="Parse"/> method to create Markdown documents.
    /// </summary>
    public static class UMarkdown
    {
        /// <summary>
        /// Convert plain text markdown into a <see cref="MarkdownVisualElement"/>, which is renderable with
        /// UIToolkit. 
        /// </summary>
        /// <param name="markdown">
        /// The plain text markdown content that will be rendered into a <see cref="MarkdownVisualElement"/>
        /// </param>
        /// <param name="context">
        /// A <see cref="UMarkdownContext"/> instance that will control the configuration and settings for how
        /// the conversion of plain text into markdown is performed.
        /// <para> </para>
        /// <para>
        /// Often, for Editor use cases, it makes sense to use either the default context using UMarkdownContext.<see cref="UMarkdownContext.FromFile(string, bool)"/>
        ///
        /// <code>UMarkdown.Parse("# Example", UMarkdownContext.FromFile(filePath: Application.dataPath, isRuntime: false))</code>
        /// <para> </para>
        /// or, if the markdown is sourced from a file, construct a context around that file using UMarkdownContext.<see cref="UMarkdownContext.FromFile(string, bool)"/>
        /// <code>UMarkdown.Parse("# Example", UMarkdownContext.FromFile(filePath: "file.md", isRuntime: false))</code>
        /// </para>
        /// <para> </para>
        /// <para>
        /// Otherwise, in Runtime, use the UMarkdownContext.<see cref="UMarkdownContext.Runtime()"/> utility function.
        /// </para>
        /// 
        /// </param>
        /// <returns></returns>
        public static MarkdownVisualElement Parse(string markdown, UMarkdownContext context)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UsePipeTables()
                .UseEmphasisExtras()
                .UseAutoLinks()
                .UseGenericAttributes()
                .Build();

            var md = Markdig.Markdown.Parse(markdown, pipeline);
            
            var element = UMarkdownConverter.Convert(md, context);

            return element;
        }
    }
}