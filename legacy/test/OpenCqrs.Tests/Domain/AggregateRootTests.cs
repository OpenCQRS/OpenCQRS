using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NUnit.Framework;
using OpenCqrs.Domain;

namespace OpenCqrs.Tests.Domain
{
    [TestFixture]
    public class AggregateRootTests
    {
        [Test]
        public void CreateTestAggregate()
        {
            var sut = new TestAggregate(10);
            Assert.AreEqual(1, sut.Events.Count, "Events");
            Assert.AreEqual(1, sut.Version, "Version");
            Assert.AreEqual(10, sut.Number, "Number");
        }

        [Test]
        public void AddItem()
        {
            var sut = new TestAggregate(100);

            sut.AddItem("Description", 1, 10.00, true);

            Assert.AreEqual(2, sut.Events.Count, "Events");
            Assert.AreEqual(2, sut.Version, "Version");
            Assert.AreEqual(1, sut.Items.Count, "Items");
            Assert.AreEqual("Description", sut.Items[0].Description, "Description");
            Assert.AreEqual(1, sut.Items[0].Quantity, "Quantity");
            Assert.AreEqual(10.00, sut.Items[0].Price, "Price");
            Assert.AreEqual(true, sut.Items[0].Taxable, "Taxable");
        }

        [Test]
        public void RemoveItem()
        {
            var sut = new TestAggregate(100);
            sut.AddItem("Description", 1, 10.00, true);

            sut.RemoveItem(1);

            Assert.AreEqual(3, sut.Events.Count, "Events");
            Assert.AreEqual(3, sut.Version, "Version");
            Assert.AreEqual(0, sut.Items.Count, "Items");
        }

        [Test]
        public void ChangeItem()
        {
            var sut = new TestAggregate(100);
            sut.AddItem("Description", 1, 10.00, true);

            sut.ChangeItem(1, "New Description");
            sut.ChangeItem(1, 10);

            Assert.AreEqual(4, sut.Events.Count, "Events");
            Assert.AreEqual(4, sut.Version, "Version");
            Assert.AreEqual(1, sut.Items.Count, "Items");
            Assert.AreEqual("New Description", sut.Items[0].Description, "Description");
            Assert.AreEqual(10, sut.Items[0].Quantity, "Quantity");
        }

        public class TestAggregate : AggregateRoot
        {
            public Int32 Number { get; private set; }
            private readonly List<Item> _lineItems = new List<Item>();
            public ReadOnlyCollection<Item> Items => _lineItems.AsReadOnly();

            public TestAggregate()
            {
            }

            public TestAggregate(Int32 number)
            {
                AddAndApplyEvent(new TestAggregateCreated(number));
            }

            private void Apply(TestAggregateCreated @event)
            {
                Number = @event.Number;
            }

            public void AddItem(String v1, Int32 v2, Double v3, Boolean v4)
            {
                AddAndApplyEvent(new ItemAdded()
                {
                    AggregateRootId = Id,
                    Description = v1,
                    Quantity = v2,
                    Price = v3,
                    Taxable = v4
                });
            }

            private void Apply(ItemAdded @event)
            {
                _lineItems.Add(new Item(Version, @event.Description, @event.Quantity, @event.Price, @event.Taxable));
            }

            public void RemoveItem(Int32 id)
            {
                AddAndApplyEvent(new ItemRemoved()
                {
                    AggregateRootId = Id,
                    ItemId = id
                });
            }

            private void Apply(ItemRemoved @event)
            {
                var item = _lineItems.SingleOrDefault(l => l.Id == @event.ItemId);
                if (item != null)
                {
                    _lineItems.Remove(item);
                }
            }

            public void ChangeItem(Int32 id, String description)
            {
                AddAndApplyEvent(new ItemDescriptionChanged()
                {
                    AggregateRootId = Id,
                    ItemId = id,
                    Description = description
                });
            }

            private void Apply(ItemDescriptionChanged @event)
            {
                var item = _lineItems.SingleOrDefault(l => l.Id == @event.ItemId);
                if (item != null)
                {
                    _lineItems.Remove(item);
                }
                _lineItems.Add(new Item(item.Id, @event.Description, item.Quantity, item.Price, item.Taxable));
            }

            public void ChangeItem(Int32 id, Int32 quantity)
            {
                AddAndApplyEvent(new ItemQuantityChanged()
                {
                    AggregateRootId = Id,
                    ItemId = id,
                    Quantity = quantity
                });
            }

            private void Apply(ItemQuantityChanged @event)
            {
                var item = _lineItems.SingleOrDefault(l => l.Id == @event.ItemId);
                if (item != null)
                {
                    _lineItems.Remove(item);
                }
                _lineItems.Add(new Item(item.Id, item.Description, @event.Quantity, item.Price, item.Taxable));
            }

        }

        public class ItemQuantityChanged : DomainEvent
        {
            public Int32 ItemId { get; set; }
            public Int32 Quantity { get; set; }
        }

        public class ItemDescriptionChanged : DomainEvent
        {
            public Int32 ItemId { get; set; }
            public String Description { get; set; }
        }

        public class ItemRemoved : DomainEvent
        {
            public Int32 ItemId { get; set; }
        }

        public class Item
        {
            public Item(Int32 id, String description, Int32 quantity, Double price, Boolean taxable)
            {
                Id = id;
                Description = description ?? throw new ArgumentNullException(nameof(description));
                Quantity = quantity;
                Price = price;
                Taxable = taxable;
            }

            public Int32 Id { get; }
            public String Description { get; }
            public Int32 Quantity { get; }
            public Double Price { get; }
            public Boolean Taxable { get; }
        }

        public class TestAggregateCreated : DomainEvent
        {
            public Int32 Number { get; set; }

            public TestAggregateCreated(Int32 number) => Number = number;
        }

        public class ItemAdded : DomainEvent
        {
            public String Description { get; set; }
            public Int32 Quantity { get; set; }
            public Double Price { get; set; }
            public Boolean Taxable { get; set; }
        }
    }
}
