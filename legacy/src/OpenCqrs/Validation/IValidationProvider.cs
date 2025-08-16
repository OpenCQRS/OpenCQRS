using System;
using System.Threading.Tasks;
using OpenCqrs.Commands;

namespace OpenCqrs.Validation
{
    public interface IValidationProvider
    {
        Task<ValidationResponse> ValidateAsync<TCommand>(TCommand command)
            where TCommand : ICommand;

        ValidationResponse Validate<TCommand>(TCommand command)
            where TCommand : ICommand;
    }

    public class DefaultValidationProvider : IValidationProvider
    {
        public Task<ValidationResponse> ValidateAsync<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            throw new NotImplementedException(Consts.ValidationRequiredMessage);
        }

        public ValidationResponse Validate<TCommand>(TCommand command)
            where TCommand : ICommand
        {
            throw new NotImplementedException(Consts.ValidationRequiredMessage);
        }
    }
}
