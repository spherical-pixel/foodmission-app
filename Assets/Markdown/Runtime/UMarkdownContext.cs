using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    [Serializable]
    public class UMarkdownContext
    {
        

        private UMarkdownContext()
        {
            
        }

        /// <summary>
        /// The runtime context uses an empty root filepath, and the project configuration.
        /// </summary>
        /// <returns></returns>
        public static UMarkdownContext Runtime()
        {
            return new UMarkdownContext
            {
                isRuntime = true,
                config = UMarkdownConfigData.LoadProjectConfig(),
                rootFilePath = ""
            };
        }
        
        /// <summary>
        /// Create a context that is file rooted to the given filepath of the markdown file, and uses the project config
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isRuntime"></param>
        /// <returns></returns>
        public static UMarkdownContext FromFile(string filePath, bool isRuntime) => FromFile(filePath, isRuntime, UMarkdownConfigData.LoadProjectConfig());

        /// <summary>
        /// Create a context that is file rooted to the given filepath, and is using a given config.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isRuntime"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        public static UMarkdownContext FromFile(string filePath, bool isRuntime, UMarkdownConfigData config)
        {
            return new UMarkdownContext
            {
                isRuntime = isRuntime,
                config = config,
                rootFilePath = File.Exists(filePath) ? Path.GetDirectoryName(filePath) : filePath
            };
        }
        
        /// <summary>
        /// Some <see cref="UMarkdownConfigData"/> configuration.
        /// </summary>
        public UMarkdownConfigData config;
        
        /// <summary>
        /// anytime files are referenced in the markdown, the path is relative to this file root path.
        /// Keep in mind that when <see cref="isRuntime"/> is true, files must be loaded from a /Resources folder,
        /// because the AssetDatabase is not available. 
        /// </summary>
        public string rootFilePath; // where files should be sourced
        
        /// <summary>
        /// it is not obvious if markdown is being compiled for runtime use, or for editor use, so the decision
        /// is left as configuration. The reason its important at all is that images cannot be loaded
        /// in the same way between environments. In runtime, they must be loaded via Resources. However, in
        /// Editor, they can be loaded using the AssetDatabase.
        ///
        /// Also, images that are URLs will simply load from the internet, always. 
        /// </summary>
        public bool isRuntime;

        /// <summary>
        /// Loading assets can be delegated to this function.
        /// Anytime an asset needs to be loaded for the markdown document, this function will be called.
        /// If you override this function, you have the ability to ingest the url from the document and convert
        /// it into a Texture2D however you want.
        /// </summary>
        public UMarkdownTextureLoader textureLoader = UMarkdownTextureLoaders.DefaultLoader;
    }

    /// <summary>
    /// A function template for loading a Texture2D. When the image is loaded, the <see cref="applier"/> callback argument should be
    /// invoked. 
    /// </summary>
    public delegate void UMarkdownTextureLoader(UMarkdownContext ctx, MarkdownLinkUri linkUri, Action<Texture2D> applier);

    public static class UMarkdownTextureLoaders
    {
        /// <summary>
        /// The default <see cref="UMarkdownTextureLoader"/> for UMarkdown will load images from the Resources folder during runtime,
        /// and from the AssetDatabase during Editor.
        /// </summary>
        public static void DefaultLoader(UMarkdownContext ctx, MarkdownLinkUri linkUri, Action<Texture2D> applier)
        {
            if (ctx.isRuntime)
            {
                LoadFromRuntimeResources(ctx, linkUri, applier); 
            }
            else
            {
                LoadFromEditorAssets(ctx, linkUri, applier);
            }
        }
        
        /// <summary>
        /// A standard <see cref="UMarkdownTextureLoader"/> that will load images from the Resources folder. 
        /// </summary>
        public static void LoadFromRuntimeResources(UMarkdownContext ctx, MarkdownLinkUri linkUri, Action<Texture2D> applier)
        {
            var texture = Resources.Load<Texture2D>(linkUri.qualifiedLink);
            applier(texture);
        }

        
        /// <summary>
        /// A standard <see cref="UMarkdownTextureLoader"/> that will load images from the AssetDatabase. This only
        /// works in Editor! If this function is attempted at Runtime, it will throw a Not Implemented Exception!
        /// </summary>
        public static void LoadFromEditorAssets(UMarkdownContext ctx, MarkdownLinkUri linkUri,
            Action<Texture2D> applier)
        {
#if UNITY_EDITOR
            var texture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(linkUri.qualifiedLink);
            applier(texture);
#else
            throw new NotImplementedException($"cannot use the {nameof(LoadFromEditorAssets)} outside of the editor");
#endif
        }
    }
    
    
    /// <summary>
    /// The config data controls how markdown should be compiled and rendered.
    /// </summary>
    [Serializable]
    public class UMarkdownConfigData
    {
        /// <summary>
        /// There can be only 1 project wide config file, and it must be called "MarkdownSettings", AND
        /// be located in a /Resources folder.
        ///
        /// This method will get the configuration specified in the project file.
        /// If no file exists, the Default configuration is used, retrieved from <see cref="UMarkdownConfig.GetDefault"/>
        /// </summary>
        /// <returns></returns>
        public static UMarkdownConfigData LoadProjectConfig()
        {
            var asset = Resources.Load<UMarkdownConfig>(UMarkdownConstants.CONFIG_FILENAME);
            if (asset == null) return UMarkdownConfig.GetDefault();

            return asset.configData;
        }
        
        /// <summary>
        /// When true, code fence blocks will have a copy button in the upper-right.
        /// </summary>
        public bool useCodeCopyButtons;
        
        /// <summary>
        /// Unity has a set of hardcoded file extensions it will always treat as a
        /// TextAsset, and this cannot be changed. However, MarkdownSupport adds a custom
        /// Inspector for TextAssets, and will render the text as markdown if the TextAsset's
        /// extension is in the list of validTextAssetExtensions.
        ///
        /// By default, the only valid extension is .md
        ///
        /// <para>
        /// (.markdown is treated as a MarkdownAsset, and IS a valid markdown extension, but IS NOT
        /// a valid TextAsset). 
        /// </para>
        /// </summary>
        public string[] validTextAssetExtensions;
        
        /// <summary>
        /// The set of style sheets that will be applied to the compiled markdown. 
        /// </summary>
        public List<StyleSheet> styleSheets;
        
        /// <summary>
        /// Optionally, a UMarkdownRichTextStyles asset can override certain styling options that
        /// aren't set in USS. By default, this is null, and when its null, default values are used.
        ///
        /// To get the actual values, use, <see cref="RichTextStyle"/>, because that will use the default
        /// values if this field is null.
        /// </summary>
        public UMarkdownRichTextStyles richTextStyleAsset;

        private UMarkdownRichTextStyleData _cachedStyle;
        
        /// <summary>
        /// The <see cref="UMarkdownRichTextStyleData"/> data to use to render the compiled markdown.
        /// </summary>
        public UMarkdownRichTextStyleData RichTextStyle
        {
            get
            {
                if (richTextStyleAsset == null)
                {
                    _cachedStyle = UMarkdownRichTextStyles.GetDefault();
                    return _cachedStyle;
                }
                return richTextStyleAsset.data;
            }
        }
    }
}