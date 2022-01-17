﻿using Kledex.Commands;
using Kledex.Sample.EventSourcing.Domain.Commands;
using System.Threading.Tasks;

namespace Kledex.Sample.EventSourcing.Domain
{
    public class ProductService : IProductService
    {
        public async Task<CommandResponse> CreateProductAsync(CreateProduct command)
        {
            var product = new Product(command.AggregateRootId, command.Name, command.Description, command.Price);

            return await Task.FromResult(new CommandResponse
            {
                Events = product.Events,
                Result = true
            });
        }
    }
}
