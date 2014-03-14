﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests.WithoutConfigurations
{
    [TestClass]
    public class AddAggregateBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldAddNewAggregateRoot_Detached()
        {
            var associated = new OneToOneAssociatedModel { Title = "Associated" };
            var manyAssociated = new OneToManyAssociatedModel { Title = "Associated" };
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "One" },
                    new OneToManyOwnedModel { Title = "Two" },
                    new OneToManyOwnedModel { Title = "Three" }
                },
                OneToManyAssociated = new List<OneToManyAssociatedModel>
                {
                    manyAssociated
                },
                OneToOneOwned = new OneToOneOwnedModel { Title = "OneToOne" },
                OneToOneAssociated = associated
            };

            using (var context = new TestDbContext())
            {
                context.OneToManyAssociatedModels.Add(manyAssociated);
                context.OneToOneAssociatedModels.Add(associated);
                context.SaveChanges();
            } // Simulate detach

            using (var context = new TestDbContext())
            {
					node1 = context.UpdateGraph(node1);

                context.SaveChanges();
                Assert.IsNotNull(context.Nodes.SingleOrDefault(p => p.Id == node1.Id));
            }
        }

        [TestMethod]
        public void ShouldAddNewAggregateRoot_Attached()
        {
            var associated = new OneToOneAssociatedModel { Title = "Associated" };
            var manyAssociated = new OneToManyAssociatedModel { Title = "Associated" };
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "One" },
                    new OneToManyOwnedModel { Title = "Two" },
                    new OneToManyOwnedModel { Title = "Three" }
                },
                OneToManyAssociated = new List<OneToManyAssociatedModel>
                {
                    manyAssociated
                },
                OneToOneOwned = new OneToOneOwnedModel { Title = "OneToOne" },
                OneToOneAssociated = associated
            };

            using (var context = new TestDbContext())
            {
                context.OneToManyAssociatedModels.Add(manyAssociated);
                context.OneToOneAssociatedModels.Add(associated);

					 node1 = context.UpdateGraph(node1);

                context.SaveChanges();
                Assert.IsNotNull(context.Nodes.SingleOrDefault(p => p.Id == node1.Id));
            }
        }

        [TestMethod]
        public void ShouldAddNewAggregateWithOwnedEntityAndOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToOneOwned = new OneToOneOwnedModel
                {
                    OneToOneOneToManyOwned = new[]
                    {
                        new OneToOneOneToManyOwnedModel {Title = "One"},
                        new OneToOneOneToManyOwnedModel {Title = "Two"},
                        new OneToOneOneToManyOwnedModel {Title = "Three"}
                    }
                }
            };

            using (var context = new TestDbContext())
            {
					node1 = context.UpdateGraph(node1);
                context.SaveChanges();
            }

            using (var context = new TestDbContext())
            {
                var reload = context.Nodes
                    .Include("OneToOneOwned.OneToOneOneToManyOwned")
                    .SingleOrDefault(p => p.Id == node1.Id);

                Assert.IsNotNull(reload);
                Assert.AreEqual(node1.Title, reload.Title);
                Assert.IsNotNull(reload.OneToOneOwned);
                Assert.AreEqual(node1.OneToOneOwned.Id, reload.OneToOneOwned.Id);

                Assert.IsNotNull(reload.OneToOneOwned.OneToOneOneToManyOwned);
                Assert.AreEqual(3, reload.OneToOneOwned.OneToOneOneToManyOwned.Count);
                Assert.AreEqual(node1.OneToOneOwned.OneToOneOneToManyOwned.First().Id, node1.OneToOneOwned.OneToOneOneToManyOwned.First().Id);

            }
        }
    }
}
