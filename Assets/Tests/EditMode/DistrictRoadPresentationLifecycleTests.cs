using System.Reflection;
using CityForgeV3.World;
using NUnit.Framework;
using UnityEngine;

public class DistrictRoadPresentationLifecycleTests
{
    [Test]
    public void ClearingWorldReleasesRoadRootAndCachedVisualState()
    {
        var host = new GameObject("Road lifecycle fixture");
        try
        {
            var world = host.AddComponent<DistrictWorldController>();
            var root = new GameObject("Previous roads");
            root.transform.SetParent(host.transform);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(DistrictWorldController);
            var rootField = type.GetField("_roadArtworkRoot", flags);
            rootField.SetValue(world, root.transform);
            var states = (System.Collections.IDictionary)type.GetField("_roadVisualState", flags).GetValue(world);
            states.Add(Vector2Int.zero, "previous road");

            type.GetMethod("ClearWorld", flags).Invoke(world, null);

            // ReferenceEquals also rejects Unity's destroyed-object fake null.
            // Play Mode keeps that old root alive until the end of the frame.
            Assert.IsTrue(ReferenceEquals(null, rootField.GetValue(world)));
            Assert.AreEqual(0, states.Count);
        }
        finally { Object.DestroyImmediate(host); }
    }
}
