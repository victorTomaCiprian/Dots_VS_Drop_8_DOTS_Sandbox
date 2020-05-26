using JetBrains.Annotations;

namespace Runtime
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public enum InterpolationType : byte
    {
        Linear,
        SmoothStep
    }
}
