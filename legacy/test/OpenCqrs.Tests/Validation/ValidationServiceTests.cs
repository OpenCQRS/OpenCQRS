using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoFixture;
using Moq;
using NUnit.Framework;
using OpenCqrs.Exceptions;
using OpenCqrs.Tests.Fakes;
using OpenCqrs.Validation;

namespace OpenCqrs.Tests.Validation
{
    [TestFixture]
    public class ValidationServiceTests
    {
        private IValidationService _sut;
        private Mock<IValidationProvider> _validationProviderMock;
        private CreateAggregate _createAggregate;
        private ValidationResponse _validationResponse;

        [SetUp]
        public void SetUp()
        {
            _createAggregate = new Fixture().Create<CreateAggregate>();
            _validationResponse = new Fixture().Build<ValidationResponse>()
                .With(x => x.Errors, new List<ValidationError>())
                .Create();

            _validationProviderMock = new Mock<IValidationProvider>();
            _validationProviderMock
                .Setup(x => x.ValidateAsync(_createAggregate))
                .ReturnsAsync(_validationResponse);
            _validationProviderMock
                .Setup(x => x.Validate(_createAggregate))
                .Returns(_validationResponse);

            _sut = new ValidationService(_validationProviderMock.Object);
        }

        [Test]
        public void ValidateAsync_ThrowsException_WhenCommandIsNull()
        {
            _createAggregate = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.ValidateAsync(_createAggregate));
        }

        [Test]
        public void Validate_ThrowsException_WhenCommandIsNull()
        {
            _createAggregate = null;
            Assert.ThrowsAsync<ArgumentNullException>(async () => await _sut.ValidateAsync(_createAggregate));
        }

        [Test]
        public async Task ValidateAsync_CallsProvider()
        {
            await _sut.ValidateAsync(_createAggregate);
            _validationProviderMock.Verify(x => x.ValidateAsync(_createAggregate), Times.Once);
        }

        [Test]
        public void Validate_CallsProvider()
        {
            _sut.Validate(_createAggregate);
            _validationProviderMock.Verify(x => x.Validate(_createAggregate), Times.Once);
        }

        [Test]
        public void ValidateAsync_ThrowsException_WhenValidationFails()
        {
            _validationResponse = new Fixture().Build<ValidationResponse>()
                .With(x => x.Errors, new List<ValidationError>
                {
                    new ValidationError
                    {
                        PropertyName = "Something",
                        ErrorMessage = "Blah blah blah..."
                    }
                })
                .Create();

            _validationProviderMock
                .Setup(x => x.ValidateAsync(_createAggregate))
                .ReturnsAsync(_validationResponse);

            _sut = new ValidationService(_validationProviderMock.Object);

            Assert.ThrowsAsync<ValidationException>(async () => await _sut.ValidateAsync(_createAggregate));
        }

        [Test]
        public void Validate_ThrowsException_WhenValidationFails()
        {
            _validationResponse = new Fixture().Build<ValidationResponse>()
                .With(x => x.Errors, new List<ValidationError>
                {
                    new ValidationError
                    {
                        PropertyName = "Something",
                        ErrorMessage = "Blah blah blah..."
                    }
                })
                .Create();

            _validationProviderMock
                .Setup(x => x.Validate(_createAggregate))
                .Returns(_validationResponse);

            _sut = new ValidationService(_validationProviderMock.Object);

            Assert.Throws<ValidationException>(() => _sut.Validate(_createAggregate));
        }
    }
}
