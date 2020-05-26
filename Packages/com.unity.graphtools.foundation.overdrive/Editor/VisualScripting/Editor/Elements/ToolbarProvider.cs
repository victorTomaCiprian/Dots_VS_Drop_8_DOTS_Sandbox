using VisualScripting.Editor.Elements.Interfaces;

namespace VisualScripting.Editor.Elements
{
    public class ToolbarProvider : IToolbarProvider
    {
        public bool ShowButton(string buttonName)
        {
            return true;
        }
    }
}
