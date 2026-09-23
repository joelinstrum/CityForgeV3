using CityForgeV3.Behaviors;
using NUnit.Framework;
using UnityEngine;

namespace CityForgeV3.Tests
{
    public sealed class DockWorkerVisualTests
    {
        [Test]
        public void AdjacentDockWorkersHaveDifferentIdleTempos()
        {
            var prefab = Resources.Load<GameObject>(
                "CityForgeV3/Characters/DockWorkerV01/DockWorkerV01");
            Assert.That(prefab, Is.Not.Null);
            var first = Object.Instantiate(prefab);
            var second = Object.Instantiate(prefab);
            try
            {
                first.AddComponent<DockWorkerVisual>().Initialize(null, 0);
                second.AddComponent<DockWorkerVisual>().Initialize(null, 1);
                var firstAnimator = first.GetComponentInChildren<Animator>();
                var secondAnimator = second.GetComponentInChildren<Animator>();
                Assert.That(firstAnimator, Is.Not.Null);
                Assert.That(secondAnimator, Is.Not.Null);
                Assert.That(firstAnimator.speed, Is.EqualTo(.94f));
                Assert.That(secondAnimator.speed, Is.EqualTo(1.06f));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
            }
        }
    }
}
