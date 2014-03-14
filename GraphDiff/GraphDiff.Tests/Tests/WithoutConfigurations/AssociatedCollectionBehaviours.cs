using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests.WithoutConfigurations
{
    [TestClass]
    public class AssociatedCollectionBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddRelationToExistingAssociatedCollection()
        {
            var associated = new OneToManyAssociatedModel { Title = "Second One" };
            var node1 = new TestNode 
            { 
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel> 
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.OneToManyAssociatedModels.Add(associated);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyAssociated.Add(associated);

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyAssociated.Count == 2);
            }
        }

        [TestMethod]
        public void ShouldRemoveRelationFromExistingAssociatedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel> 
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyAssociated.Remove(node1.OneToManyAssociated.First());

            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyAssociated.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldNotUpdateEntitesWithinAnAssociatedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyAssociated = new List<OneToManyAssociatedModel> 
                {
                    new OneToManyAssociatedModel { Title = "First One" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyAssociated.First().Title = "This should not overwrite value";

            using (var context = new TestDbContext())
            {
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyAssociated).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyAssociated.Single().Title == "First One");
            }
        }
    }
}
