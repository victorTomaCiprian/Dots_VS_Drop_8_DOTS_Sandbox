using UnityEditor.VisualScripting.Editor;
using VisualScripting.Editor.Elements.Interfaces;

namespace DotsStencil
{
    public class DotsToolbarProvider : IToolbarProvider
    {
        public bool ShowButton(string buttonName)
        {
            if (buttonName == VseMenu.BuildAllButton)
            {
                return false;
            }
            return buttonName != VseMenu.ViewInCodeViewerButton;
        }
    }
}
