﻿using Kledex.Commands;
using System.Threading.Tasks;

namespace Kledex.Validation
{
    public interface IValidationService
    {
        /// <summary>Validates the command asynchronously.</summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        Task ValidateAsync<TCommand>(TCommand command)
            where TCommand : ICommand;

        /// <summary>Validates the command.</summary>
        /// <param name="command">The command.</param>
        void Validate<TCommand>(TCommand command)
            where TCommand : ICommand;
    }
}
