using UnityEngine;

public class MathGroupNode : MathNode
{
    public string m_Title;
    public bool m_IsScope;

    public void OnEnable()
    {
        name = "MathGroupNode";
    }

    public override float Evaluate()
    {
        return 0;
    }

    public override void ResetConnections()
    {
    }
}
