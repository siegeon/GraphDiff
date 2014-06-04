﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RefactorThis.GraphDiff.Tests.Tests.WithoutConfigurations
{
    [TestClass]
    public class GuidKeyBehaviors : TestBase
    {
        [TestMethod]
        public void ShouldSupportGuidKeys()
        {
            var model = new GuidTestNode();
            using (var context = new TestDbContext())
            {
	            context.GuidKeyModels.Add(model);
                context.SaveChanges();

                // http://stackoverflow.com/questions/5270721/using-guid-as-pk-with-ef4-code-first
                Assert.IsTrue(Attribute.IsDefined(model.GetType().GetProperty("Id"), typeof(DatabaseGeneratedAttribute)));

                Assert.IsNotNull(model.Id);
                Assert.AreNotEqual(Guid.Empty, model.Id);
            } // simulate detach

            model.OneToOneOwned = new GuidOneToOneOwned();

            using (var context = new TestDbContext())
            {
                model = context.UpdateGraph(model);
                context.SaveChanges();

                Assert.IsNotNull(model.OneToOneOwned);
                Assert.IsNotNull(model.OneToOneOwned.Id);
                Assert.AreNotEqual(Guid.Empty, model.OneToOneOwned.Id);
            }
        }

        [TestMethod]
        public void ShouldSupportAddingRootWithGuidKey()
        {
            var model = new GuidTestNode {OneToOneOwned = new GuidOneToOneOwned()};

            using (var context = new TestDbContext())
            {
                model = context.UpdateGraph(model);
                context.SaveChanges();

                Assert.AreNotEqual(Guid.Empty, model.Id);
                Assert.IsNotNull(model.OneToOneOwned);
                Assert.IsNotNull(model.OneToOneOwned.Id);
                Assert.AreNotEqual(Guid.Empty, model.OneToOneOwned.Id);
            }
        }
    }
}
