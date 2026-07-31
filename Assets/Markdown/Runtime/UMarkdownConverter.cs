using System;
using System.Collections.Generic;
using Markdig.Extensions.Tables;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using UnityEngine;
using UnityEngine.UIElements;
using Highlighter = Highlight.Highlighter;

namespace BrewedInk.MarkdownSupport
{
    /// <summary>
    /// The UMarkdownConverter class is the mean and potatoes of the MarkdownSupport tool.
    /// All the methods have been left as public static to make them easy to extend and utilise on
    /// your own.
    /// <para>
    /// However, <b>the recommended approach is to not use this class directly</b>. Instead, use <see cref="UMarkdown.Parse"/>
    /// </para>
    /// </summary>
    public static class UMarkdownConverter
    {

        public static string CodeMSpaceValue = 
#if UNITY_2022_1_OR_NEWER
            ".7em";
#else 
            "24em";
#endif

        public static string StandardLineHeight = $"1.6em";
        
        /// <summary>
        /// Converts a MarkDig MarkdownDocument into a visual element.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static MarkdownVisualElement Convert(MarkdownDocument document, UMarkdownContext context)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (context == null) throw new ArgumentNullException(nameof(context));
            
            var root = new MarkdownVisualElement();
            root.AddToClassList("root");
            var isFirst = true;
            foreach (var block in document)
            {
                if (TryConvertBlock(context, block, out var element))
                {
                    if (isFirst)
                    {
                        element.AddToClassList("first-block");
                        isFirst = false;
                    }
                    root.Add(element);
                }
            }

            foreach (var style in context.config.styleSheets)
            {
                root.styleSheets.Add(style);
            }
            
            return root;
        }


        public static bool TryConvertBlock(UMarkdownContext context, Block block, out VisualElement element)
        {
            element = null;
            switch (block)
            {
                case HeadingBlock heading:
                    element = Convert(context, heading);
                    break;
                case ParagraphBlock paragraphBlock:
                    element = ConvertLeaf(context, paragraphBlock);
                    break;
                case QuoteBlock quoteBlock:
                    element = Convert(context, quoteBlock);
                    break;
                case FencedCodeBlock codeBlock:
                    element = Convert(context, codeBlock);
                    break;
                case ListBlock listBlock:
                    element = Convert(context, listBlock);
                    break;
                case ListItemBlock listItemBlock:
                    element = Convert(context, listItemBlock);
                    break;
                case ThematicBreakBlock lineBlock:
                    element = Convert(context, lineBlock);
                    break;
                case Table table:
                    element = Convert(context, table);
                    break;
                case TableRow tableRow:
                    element = Convert(context, tableRow);
                    break;
                case TableCell tableCell:
                    element = Convert(context, tableCell);
                    break;
                default:
                    element = new Label("unknown block " + block);
                    break;
            }

            ApplyAttributes(block, element);
            return element != null;
        }

        public static void ApplyAttributes(Block block, VisualElement element)
        {
            if (element == null) return;

            element.AddToClassList("block");

            var attrs = block.TryGetAttributes();
            if (attrs == null) return;

            if (attrs.Properties != null)
            {
                foreach (var kvp in attrs.Properties)
                {
                    element.SetMarkdownProperty(kvp.Key, kvp.Value);
                }
            }

            if (attrs.Id != null)
            {
                element.name = attrs.Id;
            }

            if (attrs.Classes != null)
            {
                foreach (var clazz in attrs.Classes)
                {
                    element.AddToClassList(clazz);
                }
            }
            
        }

        public static VisualElement Convert(UMarkdownContext context, HeadingBlock block)
        {
            // var lbl = new SelectableLabel(GetInlineText(context, block.Inline));

            var lbl = ConvertLeaf(context, block);
            lbl.AddToClassList("heading");
            lbl.AddToClassList($"heading-{block.Level}");

            var firstLabel = lbl.Q<SelectableLabel>();
            if (firstLabel != null)
            {
                lbl.name = firstLabel.text.Replace(" ", "-").ToLowerInvariant();
            }
            return lbl;
        }

        public static VisualElement Convert(UMarkdownContext context, TableCell cell)
        {
            var root = new VisualElement();
            root.AddToClassList("table-cell");
            
            foreach (var element in cell)
            {
                if (!TryConvertBlock(context, element, out var subBlock))
                {
                    continue;
                }
                
                root.Add(subBlock);
            }

            return root;
        }

        public static VisualElement Convert(UMarkdownContext context, TableRow row)
        {
            var root = new VisualElement();
            root.AddToClassList("table-row");
            var columnNumber = 0;
            var cells = new List<VisualElement>();
            if (row.IsHeader)
            {
                root.AddToClassList("header");
            }
            

            var cellContainer = new VisualElement();
            cellContainer.AddToClassList("row-data");
            // root.Add(cellContainer);
            foreach (var element in row)
            {
                if (!TryConvertBlock(context, element, out var subBlock))
                {
                    continue;
                }
                subBlock.AddToClassList("table-col");
                subBlock.AddToClassList("table-col-" + (columnNumber++));
                if (row.IsHeader)
                {
                    subBlock.AddToClassList("header");
                }
                cells.Add(subBlock);
                root.Add(subBlock);
            }

            foreach (var cell in cells)
            {
                cell.RegisterCallback<GeometryChangedEvent>(evt =>
                {
                    cell.style.width = root.localBound.width / cells.Count;
                });
            }
            cells[cells.Count - 1].AddToClassList("last-entry");

            return root;
        }

        public static VisualElement Convert(UMarkdownContext context, Table table)
        {
            var root = new VisualElement();
            root.AddToClassList("table");
            foreach (var element in table)
            {
                if (!TryConvertBlock(context, element, out var subBlock))
                {
                    continue;
                }
                root.Add(subBlock);
            }

            return root;
        }

        public static VisualElement Convert(UMarkdownContext context, ThematicBreakBlock _)
        {
            var element = new VisualElement();
            element.AddToClassList("thematic-line");
            return element;
        }

        public static VisualElement Convert(UMarkdownContext context, ListItemBlock block)
        {
            var root = new VisualElement();
            foreach (var element in block)
            {
                if (!TryConvertBlock(context, element, out var subBlock))
                {
                    continue;
                }
                root.Add(subBlock);
            }

            return root;
        }
        
        public static VisualElement Convert(UMarkdownContext context, ListBlock block)
        {
            // new ListE
            var root = new VisualElement();
            foreach (var element in block)
            {
                
                if (!TryConvertBlock(context, element, out var subBlock))
                {
                    continue;
                }

                var group = new VisualElement();
                group.AddToClassList("list-element");

                if (!block.IsOrdered)
                {
                    var label = new Label();
                    label.AddToClassList("list-bullet");
                    group.Add(label);
                }
                else
                {
                    var label = new Label();
                    label.AddToClassList("list-number");
                    label.text = ((ListItemBlock)element).Order + ".";
                    group.Add(label);
                }

                group.Add(subBlock);
                
                root.Add(group);
            }
            return root;
        }

        public static VisualElement Convert(UMarkdownContext context, FencedCodeBlock block)
        {
            var code = block.Lines.ToString();
            var codeElement = new VisualElement();
            codeElement.AddToClassList("code-block");


            var codeLang = HighlightUtil.CodeLangToName(block.Info);
            var hl = new Highlighter(new RichTextEngine());
            var codeString = hl.Highlight(codeLang, code);

            codeString = $"<font=\"{context.config.RichTextStyle.codeFontAsset}\"><mspace={CodeMSpaceValue}>{codeString}</mspace></font>";
            
            var lbl = new SelectableLabel(codeString);
            lbl.AddToClassList("code-block-text");
            lbl.selection.selectionColor = context.config.RichTextStyle.codeSelectionColor;

            if (context.config.useCodeCopyButtons)
            {
                var copyButton = new Button(() => { GUIUtility.systemCopyBuffer = code; });
                copyButton.tooltip = "copy";
                copyButton.AddToClassList("copy-button");

                codeElement.Add(copyButton);
            }
            codeElement.Add(lbl);

            return codeElement;
        }

        public static VisualElement Convert(UMarkdownContext context, QuoteBlock block)
        {
            var container = new VisualElement();
            container.AddToClassList("quote-block");
            foreach (var subBlock in block)
            {
                if (TryConvertBlock(context, subBlock, out var inner))
                {
                    container.Add(inner);
                }
            }

            return container;
        }

        public static string GetInlineText(UMarkdownContext context, Inline inline, Action<LinkInline> insertLink)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    return $"<line-height={StandardLineHeight}>{literal.Content.ToString()}";
                case LinkInline link when insertLink != null:
                    insertLink?.Invoke(link);
                    return ""; // TODO: This is a known bug, that you cannot style links :( 
                case EmphasisInline emphasis:
                    if (emphasis.DelimiterChar == '~')
                    {
                        return $"<s>{GetInlineText(context, emphasis.FirstChild, insertLink)}</s>";
                    }
                    
                    
                    if (emphasis.DelimiterCount == 1)
                    {
                        return $"<i>{GetInlineText(context, emphasis.FirstChild, insertLink)}</i>";
                    }
                    else
                    {
                        return $"<b>{GetInlineText(context, emphasis.FirstChild, insertLink)}</b>";
                    }
                case ContainerInline container:
                    return GetInlineText(context, container.FirstChild, insertLink);
                case HtmlInline html:
                    return html.Tag;
                case CodeInline code:
                    return $"<font=\"{context.config.RichTextStyle.codeFontAsset}\"><mark=#{ColorUtility.ToHtmlStringRGBA(context.config.RichTextStyle.codeMarkupColor)}><mspace={CodeMSpaceValue}><size=110%>{code.Content}</size></mspace></mark></font>";
                default:
                    return $"(unknown {inline})";
            }
        }
        

        public static VisualElement ConvertLeaf(UMarkdownContext context, LeafBlock block)
        {
            var paragraphElement = new VisualElement();
            paragraphElement.AddToClassList("paragraph");
            
            VisualElement lineElement = null;
            Label lineLabel = null;
            void AddLine()
            {
                lineElement = new VisualElement();
                lineElement.AddToClassList("line");
                paragraphElement.Add(lineElement);
                lineLabel = new SelectableLabel();
                lineElement.Add(lineLabel);

            }
            AddLine();

            void InsertLink(LinkInline link)
            {
                var title= GetInlineText(context, link.FirstChild, null);

                var linkUrl = new MarkdownLinkUri(link.Url, context);
                        
                if (link.FirstChild is LinkInline subLink)
                {
                    var img = new LinkImageElement(context, lineElement, title, new MarkdownLinkUri(subLink.Url, context));
                    lineElement.Add(img);
                            
                    img.AddOpenUrlClick(linkUrl);
                    AddLine();
                    return;
                }

                if (link.IsImage)
                {
                    var img = new LinkImageElement(context, lineElement, title, linkUrl);
                    lineElement.Add(img);
                    AddLine();
                    return;
                }
                        
                var linkElement = new LinkElement(context, lineElement, lineLabel, title, linkUrl, link.Title);
                lineElement.Add(linkElement);
            }
            
            foreach (var inlineElem in block.Inline)
            {
                switch (inlineElem)
                {
                    case LineBreakInline lb:
                        if (lb.IsHard)
                        {
                            lineLabel.text += "<br>";
                        }
                        else
                        {
                            lineLabel.text += " ";
                        }
                        
                        break;
                    case LinkInline link:
                        InsertLink(link);
                        break;
                    default:
                        var text = GetInlineText(context, inlineElem, InsertLink);
                        lineLabel.text += text;
                        break;
                }
            }
            
            return paragraphElement;
        }
    }
}