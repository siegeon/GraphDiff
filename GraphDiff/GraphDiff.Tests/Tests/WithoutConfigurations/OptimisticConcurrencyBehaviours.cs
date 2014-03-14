﻿using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RefactorThis.GraphDiff.Tests.Models;

namespace RefactorThis.GraphDiff.Tests.Tests.WithoutConfigurations
{
    [TestClass]
    public class OptimisticConcurrencyBehaviours : TestBase
    {
        [TestMethod]
        [ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void ShouldThrowDbUpdateConcurrencyExceptionIfEditingOutOfDateModel()
        {
            TestNode node;
            using (var db = new TestDbContext())
            {
                node = new TestNode { Title = "Hello" };
                db.Nodes.Add(node);
                db.SaveChanges();
            }

            using (var db = new TestDbContext())
            {
                var node2 = new TestNode
                {
                    RowVersion = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x1, 0x1, 0x1, 0x1 },
                    Id = node.Id,
                    Title = "Test2"
                };

                db.UpdateGraph(node2);
                db.SaveChanges();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void ShouldThrowDbUpdateConcurrencyExceptionIfEditingNestedOutOfDateModel()
        {
            TestNode node;
            using (var db = new TestDbContext())
            {
                node = new TestNode
                {
                    Title = "Hello",
                    OneToManyOwned = new List<OneToManyOwnedModel>
                    {
                        new OneToManyOwnedModel { Title = "Test1" },
                        new OneToManyOwnedModel { Title = "Test2" }
                    }
                };
                db.Nodes.Add(node);
                db.SaveChanges();
            }

            using (var db = new TestDbContext())
            {
                node.OneToManyOwned.First().RowVersion = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x1 };
                db.UpdateGraph(node);
                db.SaveChanges();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DbUpdateConcurrencyException))]
        public void ShouldThrowDbUpdateConcurrencyExceptionWithEmptyRowVersion()
        {
            TestNode node;
            using (var db = new TestDbContext())
            {
                node = new TestNode
                {
                    Title = "Hello",
                    OneToManyOwned = new List<OneToManyOwnedModel>
                    {
                        new OneToManyOwnedModel { Title = "Test1" },
                        new OneToManyOwnedModel { Title = "Test2" }
                    }
                };
                db.Nodes.Add(node);
                db.SaveChanges();
            }

            using (var db = new TestDbContext())
            {
                node.OneToManyOwned.First().RowVersion = null;
                db.UpdateGraph(node);
                db.SaveChanges();
            }
        }
    }
}
