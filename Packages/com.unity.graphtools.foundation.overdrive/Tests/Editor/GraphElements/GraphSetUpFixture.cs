using NUnit.Framework;
using UnityEditor.GraphViewTestUtilities;

namespace Unity.GraphElementsTests
{
    [SetUpFixture] // Need to forward this for NUnit to pick it up
    public class GraphSetUpFixture : GraphViewTestEnvironment {}
}
