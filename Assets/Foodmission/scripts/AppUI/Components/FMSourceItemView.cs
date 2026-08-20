using System;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Properties;
using UnityEngine;
using UnityEngine.UIElements;
using Button = Unity.AppUI.UI.Button;
using Text = Unity.AppUI.UI.Text;

namespace eu.foodmission.platform.Components
{
    /// <summary>
    /// Reusable component for displaying bibliographic sources, citations, and interactive web/DOI links.
    /// Automatically cleans URLs from citation text and presents clickable action buttons to open external references.
    /// </summary>
    [UxmlElement]
    public partial class FMSourceItemView : ExVisualElement
    {
        private const string RootClass = "fm-source-item";
        private const string HeaderClass = "fm-source-item-header";
        private const string PrefixClass = "fm-source-item-prefix";
        private const string CitationClass = "fm-source-item-citation";
        private const string LinksContainerClass = "fm-source-item-links";
        private const string LinkButtonClass = "fm-source-item-link-btn";

        /* ========= INTERNAL UI ELEMENTS ========= */
        private readonly VisualElement _headerContainer;
        private readonly Text _prefixLabel;
        private readonly Text _citationLabel;
        private readonly VisualElement _linksContainer;

        /* ========= STATE ========= */
        private string _rawSource;
        private SourceInfo _sourceInfo;

        /* ========= EVENTS ========= */
        /// <summary>
        /// Event invoked when an external source link is clicked. Passes the URL as parameter.
        /// </summary>
        public event Action<string> LinkClicked;

        /* ========= UXML ATTRIBUTES ========= */
        [UxmlAttribute("raw-source")]
        [CreateProperty]
        public string RawSource
        {
            get => _rawSource;
            set
            {
                _rawSource = value;
                SetSource(value);
            }
        }

        [UxmlAttribute("citation-text")]
        [CreateProperty]
        public string CitationText
        {
            get => _citationLabel?.text ?? string.Empty;
            set
            {
                if (_citationLabel != null)
                {
                    _citationLabel.text = value ?? string.Empty;
                    UpdateVisibility();
                }
            }
        }

        [UxmlAttribute("show-prefix")]
        [CreateProperty]
        public bool ShowPrefix
        {
            get => _headerContainer != null && _headerContainer.style.display != DisplayStyle.None;
            set
            {
                if (_headerContainer != null)
                {
                    _headerContainer.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        [UxmlAttribute("prefix-text")]
        [CreateProperty]
        public string PrefixText
        {
            get => _prefixLabel?.text ?? string.Empty;
            set
            {
                if (_prefixLabel != null)
                {
                    _prefixLabel.text = value ?? string.Empty;
                }
            }
        }

        public SourceInfo SourceInfo => _sourceInfo;

        public FMSourceItemView()
        {
            AddToClassList(RootClass);

            // 1. Header (optional prefix e.g. "Fuente:")
            _headerContainer = new VisualElement();
            _headerContainer.AddToClassList(HeaderClass);
            Add(_headerContainer);

            _prefixLabel = new Text { text = "Fuente:" };
            _prefixLabel.AddToClassList(PrefixClass);
            _headerContainer.Add(_prefixLabel);

            // 2. Citation Text
            _citationLabel = new Text();
            _citationLabel.AddToClassList(CitationClass);
            Add(_citationLabel);

            // 3. Links Container (holds interactive link buttons/chips)
            _linksContainer = new VisualElement();
            _linksContainer.AddToClassList(LinksContainerClass);
            Add(_linksContainer);

            // Default state: hidden until source data is provided
            style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Sets and parses a raw source string containing citation text and optional URL/Markdown link.
        /// </summary>
        public void SetSource(string rawSource)
        {
            _rawSource = rawSource;
            if (string.IsNullOrWhiteSpace(rawSource))
            {
                Clear();
                return;
            }

            var parsed = LinkHelper.ParseSource(rawSource);
            SetSource(parsed);
        }

        /// <summary>
        /// Populates the component with pre-parsed SourceInfo.
        /// </summary>
        public void SetSource(SourceInfo sourceInfo)
        {
            _sourceInfo = sourceInfo;
            if (sourceInfo == null || sourceInfo.IsEmpty)
            {
                Clear();
                return;
            }

            _citationLabel.text = sourceInfo.CitationText ?? string.Empty;
            _citationLabel.style.display = !string.IsNullOrWhiteSpace(sourceInfo.CitationText)
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            RebuildLinks(sourceInfo.Links);
            style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Sets citation text and a single external link directly.
        /// </summary>
        public void SetSource(string citationText, string url, string linkTitle = null)
        {
            if (string.IsNullOrWhiteSpace(citationText) && string.IsNullOrWhiteSpace(url))
            {
                Clear();
                return;
            }

            var info = new SourceInfo
            {
                CitationText = citationText?.Trim() ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(url))
            {
                info.Links.Add(new ExtractedLink
                {
                    Title = !string.IsNullOrWhiteSpace(linkTitle) ? linkTitle : "Enlace ↗",
                    Url = url.Trim()
                });
            }

            SetSource(info);
        }

        /// <summary>
        /// Clears all content and hides the component.
        /// </summary>
        public new void Clear()
        {
            _rawSource = null;
            _sourceInfo = null;
            if (_citationLabel != null) _citationLabel.text = string.Empty;
            if (_linksContainer != null) _linksContainer.Clear();
            style.display = DisplayStyle.None;
        }

        private void RebuildLinks(List<ExtractedLink> links)
        {
            _linksContainer.Clear();
            if (links == null || links.Count == 0)
            {
                _linksContainer.style.display = DisplayStyle.None;
                return;
            }

            _linksContainer.style.display = DisplayStyle.Flex;

            foreach (var link in links)
            {
                if (string.IsNullOrWhiteSpace(link.Url)) continue;

                var btn = new Button
                {
                    //title = !string.IsNullOrWhiteSpace(link.Title) ? $"🔗 {link.Title}" : $"🔗 {link.Url} ↗"
                    title = $"🔗 {link.Url}"
                };
                btn.AddToClassList(LinkButtonClass);
                btn.variant = ButtonVariant.Accent;
                btn.size = Size.S;
                btn.quiet = true;

                string targetUrl = link.Url;
                btn.clicked += () => HandleLinkClick(targetUrl);

                _linksContainer.Add(btn);
            }
        }

        private void HandleLinkClick(string url)
        {
            Debug.LogError("[HandleLinkClick] - " + url);
            if (string.IsNullOrWhiteSpace(url)) return;

            LinkClicked?.Invoke(url);

            try
            {
                Application.OpenURL(url);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[FMSourceItemView] Error opening URL '{url}': {ex.Message}");
            }
        }

        private void UpdateVisibility()
        {
            bool hasCitation = !string.IsNullOrWhiteSpace(_citationLabel?.text);
            bool hasLinks = _sourceInfo != null && _sourceInfo.HasLinks;

            style.display = (hasCitation || hasLinks) ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
