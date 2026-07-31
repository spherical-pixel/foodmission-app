using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    public class LinkImageElement : VisualElement
    {
        private VisualElement _line;
        private Sprite _sprite;
        private UMarkdownContext _context;
        private MarkdownLinkUri _uri;
        public LinkImageElement(UMarkdownContext context, VisualElement line, string title, MarkdownLinkUri uri)
        {
            _context = context;
            _line = line;
            tooltip = title;
            _uri = uri;

            schedule.Execute(() =>
            {
                try
                {
                    GetImage();
                }
                catch (Exception ex)
                {
                    Add(new Label("Texture error. " + ex.Message));
                }
            });
        }
        
        MarkdownVisualElement FindRoot()
        {
            var candidate = parent;
            while (candidate != null)
            {
                if (candidate is MarkdownVisualElement root)
                    return root;
                candidate = candidate.parent;
            }
            return null;
        }

        public void AddOpenUrlClick(MarkdownLinkUri linkUri)
        {
            AddOnClick(() =>
            {
                linkUri.Open(FindRoot());
            });
        }

        public void AddOnClick(Action action)
        {
            AddToClassList("clickable");
            RegisterCallback<MouseDownEvent>(evt =>
            {
                action();
            });
            
        }

        void Size()
        {
            var maxWidth = _line.localBound.width;

            var x = Mathf.Min(maxWidth, _sprite.texture.width);
            var aspect = _sprite.texture.width / (float)_sprite.texture.height;

            
            var y = x / aspect;
            this.style.width = x;
            this.style.height = y;
        }

        void GetImage()
        {
            Debug.Log(_uri.type);
            Debug.Log(_uri.qualifiedLink);
            switch (_uri.type)
            {
                case MarkdownLinkType.Web:
                    DownloadImage();
                    break;
                case MarkdownLinkType.File:
                    LoadImage();
                    break;
                case MarkdownLinkType.Local:
                    Add(new Label("invalid image link format. Must be a file path or url; cannot be a document anchor."));
                    break;
            }
        }

        void LoadImage()
        {
            _context.textureLoader(_context, _uri, ApplyTexture);
        }

        void DownloadImage()
        {
            var www = UnityWebRequestTexture.GetTexture(_uri.qualifiedLink);
            var op = www.SendWebRequest();

            op.completed += (_) =>
            {
                if (www.result != UnityWebRequest.Result.Success) {
                    Add(new Label("failed to get image"));
                }
                else {
                    var tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
                    ApplyTexture(tex);
                }
            };
            
        }

        void ApplyTexture(Texture2D tex)
        {
            if (tex == null) throw new Exception("No texture available");
            _sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero);
            this.style.backgroundImage = new StyleBackground(_sprite);
            Size();
            RegisterCallback<GeometryChangedEvent>(evt => Size());
        }
    }
}