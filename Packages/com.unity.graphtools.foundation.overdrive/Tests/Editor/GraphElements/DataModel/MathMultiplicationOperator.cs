using UnityEngine;

public class MathMultiplicationOperator : MathOperator
{
    public void OnEnable()
    {
        name = "Multiply";
    }

    public override float Evaluate()
    {
        if (left != null && right != null)
            return left.Evaluate() * right.Evaluate();
        else
            return 0;
    }
}
