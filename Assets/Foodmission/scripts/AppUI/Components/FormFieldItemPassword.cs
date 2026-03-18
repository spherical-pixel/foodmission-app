using Unity.AppUI.UI;
using UnityEngine.UIElements;
using Unity.Properties;

namespace eu.foodmission.platform.Components
{
    [UxmlElement]
    public partial class FormFieldItemPassword : FormFieldItemTextField
    {

        public FormFieldItemPassword() : base()
        {
            TextFieldIsPassword = true;
            IconButtonVisible = true;
            IconButtonIcon = "eye";
            IconButtonQuiet = true;
            _iconButton.clicked += IconButtonClicked;
        }

        private void IconButtonClicked()
        {
            TextFieldIsPassword = !TextFieldIsPassword;
            IconButtonIcon = TextFieldIsPassword ? "eye" : "eye-slash";
        }
    }
}