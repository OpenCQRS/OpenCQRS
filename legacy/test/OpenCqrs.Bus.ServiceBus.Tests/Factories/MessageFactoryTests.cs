using Newtonsoft.Json;
using NUnit.Framework;
using OpenCqrs.Bus.ServiceBus.Factories;
using OpenCqrs.Bus.ServiceBus.Tests.Fakes;

namespace OpenCqrs.Bus.ServiceBus.Tests.Factories
{
    public class MessageFactoryTests
    {
        private MessageFactory _sut;

        [SetUp]
        public void Setup()
        {
            _sut = new MessageFactory();
        }

        [Test]
        public void CreateMessage_SerializesCorrectly()
        {
            var someMessage = new SomeBusMessage();
            var message = _sut.CreateMessage(someMessage);
            Assert.NotNull(message);
            Assert.NotNull(message.Body);
            Assert.Greater(message.Body.Length, 0);
            Assert.True(message.UserProperties.ContainsKey(MessageFactory.AssemblyQualifiedNamePropertyName));
        }

        [Test]
        public void CreateMessage_DeserializesCorrectType()
        {
            var someMessage = new SomeBusMessage();
            var busMessage = _sut.CreateMessage(someMessage);
            var messageType = System.Type.GetType(busMessage.UserProperties[MessageFactory.AssemblyQualifiedNamePropertyName] as string);
            var deserializedMessage = JsonConvert.DeserializeObject(System.Text.Encoding.UTF8.GetString(busMessage.Body), messageType);
            Assert.AreEqual(someMessage.GetType(), deserializedMessage.GetType());
        }
    }
}