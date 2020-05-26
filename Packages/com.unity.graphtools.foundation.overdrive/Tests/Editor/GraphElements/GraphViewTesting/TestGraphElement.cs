using Unity.GraphElements;
using UnityEngine.UIElements;

namespace UnityEditor.GraphViewTestUtilities
{
    public class TestGraphElement : GraphElement
    {
        Label m_Text;

        public TestGraphElement()
        {
            m_Text = new Label();
            Add(m_Text);
        }
    }
}
