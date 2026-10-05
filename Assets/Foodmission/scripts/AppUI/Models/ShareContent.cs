using UnityEngine;

namespace eu.foodmission.platform
{
    /// <summary>
    /// What the native share sheet receives. Image is optional; some apps keep only the image or only the text when both are sent.
    /// </summary>
    public class ShareContent
    {
        public string Text;
        public string Subject;
        public Sprite Image;

        public bool IsEmpty => string.IsNullOrWhiteSpace(Text) && Image == null;
    }
}
