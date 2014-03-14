﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests.WithoutConfigurations
{
    [TestClass]
    public class OwnedCollectionBehaviours : TestBase
    {
        [TestMethod]
        public void ShouldUpdateItemInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.First().Title = "What's up";
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var owned = node2.OneToManyOwned.First();
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "What's up");
            }
        }

        [TestMethod]
        public void ShouldAddNewItemInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            var newModel = new OneToManyOwnedModel { Title = "Hi" };
            node1.OneToManyOwned.Add(newModel);
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 2);
                var owned = context.OneToManyOwnedModels.Single(p => p.Id == newModel.Id);
                Assert.IsTrue(owned.OneParent == node2 && owned.Title == "Hi");
            }
        }

        [TestMethod]
        public void ShouldRemoveItemsInOwnedCollection()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" },
                    new OneToManyOwnedModel { Title = "Hello2" },
                    new OneToManyOwnedModel { Title = "Hello3" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            using (var context = new TestDbContext())
            {
                // Setup mapping
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldRemoveItemsInOwnedCollectionWhenSetToNull()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "Hello" },
                    new OneToManyOwnedModel { Title = "Hello2" },
                    new OneToManyOwnedModel { Title = "Hello3" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned = null;
            using (var context = new TestDbContext())
            {
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                Assert.IsTrue(node2.OneToManyOwned.Count == 0);
            }
        }

        [TestMethod]
        public void ShouldMergeTwoCollectionsAndDecideOnUpdatesDeletesAndAdds()
        {
            var node1 = new TestNode
            {
                Title = "New Node",
                OneToManyOwned = new List<OneToManyOwnedModel>
                {
                    new OneToManyOwnedModel { Title = "This" },
                    new OneToManyOwnedModel { Title = "Is" },
                    new OneToManyOwnedModel { Title = "A" },
                    new OneToManyOwnedModel { Title = "Test" }
                }
            };

            using (var context = new TestDbContext())
            {
                context.Nodes.Add(node1);
                context.SaveChanges();
            } // Simulate detach

            node1.OneToManyOwned.Remove(node1.OneToManyOwned.First());
            node1.OneToManyOwned.First().Title = "Hello";
            node1.OneToManyOwned.Add(new OneToManyOwnedModel { Title = "Finish" });
            using (var context = new TestDbContext())
            {
                context.UpdateGraph(node1);

                context.SaveChanges();
                var node2 = context.Nodes.Include(p => p.OneToManyOwned).Single(p => p.Id == node1.Id);
                Assert.IsNotNull(node2);
                var list = node2.OneToManyOwned.ToList();
                Assert.IsTrue(list[0].Title == "Hello");
                Assert.IsTrue(list[1].Title == "A");
                Assert.IsTrue(list[2].Title == "Test");
                Assert.IsTrue(list[3].Title == "Finish");
            }
        }

    }
}
