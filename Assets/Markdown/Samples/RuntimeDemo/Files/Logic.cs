using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace BrewedInk.MarkdownSupport
{
    public class Logic : MonoBehaviour
    {
        [Header("Asset references")]
        public MarkdownAsset markdown;
        public UIDocument document;
        public StyleSheet styleSheet;

        public List<Texture2D> availableTextures;

        [Header("Scene references")]
        public Transform rotatingPlane;
        public float rotateSpeed = 2;
        
        void Start()
        {
            // get the default runtime context
            var context = UMarkdownContext.Runtime();
            
            // apply a custom image loader, so that images outside of Resources can be loaded.
            context.textureLoader = CustomImageLoader;
            
            // convert the markdown asset into a visual element
            var markdownElement = markdown.BuildVisualElement(context);
            
            // add some custom styling to make it look nice in world space
            markdownElement.styleSheets.Add(styleSheet);
            
            // build an inline scroller, and add that too
            var scroller = new ScrollView();
            scroller.Add(markdownElement);
            document.rootVisualElement.Add(scroller);
            
            // set up mouse interactions. Map the screen space to UI space. 
            document.panelSettings.SetScreenToPanelSpaceFunction(_ =>
            {
                // acknowledgement: https://www.youtube.com/watch?v=gXx_j-6z8jY 
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (!Physics.Raycast(ray, out var hit, 100f, LayerMask.GetMask("UI")))
                {
                    return Vector2.zero;
                }

                var uv = hit.textureCoord;
                uv.y = 1 - uv.y;
                uv.x *= document.panelSettings.targetTexture.width;
                uv.y *= document.panelSettings.targetTexture.height;
                return uv;
            });
        }

        private void Update()
        {
            // wiggle the UI, to add some visual flair. 
            var angle = -170 + 20 * Mathf.Sin(rotateSpeed * Time.realtimeSinceStartup)  ;
            rotatingPlane.transform.rotation = Quaternion.Euler(
                x: rotatingPlane.transform.rotation.eulerAngles.x, 
                y: angle, 
                z: rotatingPlane.transform.rotation.eulerAngles.z);
        }

        void CustomImageLoader(UMarkdownContext ctx, MarkdownLinkUri uri, Action<Texture2D> applier)
        {
            /*
             * by default, the runtime context only allows you load images from the Resources folder,
             * because otherwise, it isn't obvious HOW to load a texture in a completely
             * generalized way.
             *
             * So instead of trying to generalize it, this function can be set up to load
             * images however it makes sense in your unique game/app.
             *
             * In this demo, I'm making the assumption that any link that starts with a "t:"
             * is a special image that should be loaded from the list of `availableTextures` set
             * up on the behaviour.
             */
            const string magicalPrefix = "t:";
            var startsWithPrefix = uri?.qualifiedLink?.StartsWith(magicalPrefix);
            if (!startsWithPrefix.HasValue || !startsWithPrefix.Value)
            {
                // default to base case, and assume it'll be a resource.
                UMarkdownTextureLoaders.LoadFromRuntimeResources(ctx, uri, applier);
            }
            else
            {
                /*
                 * this is the special case! To load the image,
                 * the contents after "t:" should be the name of asset in the
                 * `availableTextures` list.
                 */
                var requestedAssetName = uri.qualifiedLink.Substring(magicalPrefix.Length);
                var texture = availableTextures.FirstOrDefault(t => t.name == requestedAssetName);
                
                
                /*
                 * when we have the texture, we need to call the `applier` function to actually set it in
                 * the markdown.
                 *
                 * If the use-case requires async loading, then this callback can be called later.
                 * Be careful not to set the texture more than once.
                 */
                applier(texture);
            }
        }
    }
}
