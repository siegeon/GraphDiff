﻿using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests.WithoutConfigurations
{
    [TestClass]
    public class AssociatedEntityBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddRelationIfPreviousValueWasNull()
        {
            var node1 = new TestNode { Title = "New Node" };
            var associated = new OneToOneAssociatedModel { Title = "Associated Node" };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.OneToOneAssociatedModels.Add(associated);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneAssociated = associated;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
            }
        }

        [TestMethod]
        public void ShouldRemoveAssociatedRelationIfNull()
        {
            var node1 = new TestNode { Title = "New Node", OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Node" } };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneAssociated = null;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated == null);
            }
        }

        [TestMethod]
        public void ShouldReplaceReferenceIfNewEntityIsNotPreviousEntity()
        {
            var node1 = new TestNode { 
                Title = "New Node",
                OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Node" }
            };
            var otherModel = new OneToOneAssociatedModel { Title = "Hello" };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.OneToOneAssociatedModels.Add(otherModel);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToOneAssociated = otherModel;

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
                // should not delete it as it is associated and no cascade rule set.
                Assert.IsTrue(context.OneToOneAssociatedModels.Single(p => p.Id != otherModel.Id).OneParent == null);                
            }
        }

        [TestMethod]
        public void ShouldNotUpdatePropertiesOfAnAssociatedEntity()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneAssociated = new OneToOneAssociatedModel { Title = "Associated Node" }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach


            node1.OneToOneAssociated.Title = "Updated Content";

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToOneAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToOneAssociated.OneParent == node2);
                // should not delete it as it is associated and no cascade rule set.
                Assert.IsTrue(node2.OneToOneAssociated.Title == "Associated Node");
            }
        }
    }
}
