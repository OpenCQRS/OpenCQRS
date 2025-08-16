using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCqrs.Sample.EventSourcing.Domain.Commands;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;
using OpenCqrs.Sample.EventSourcing.Reporting.Queries;

namespace OpenCqrs.Sample.EventSourcing.Pages
{
    public class ListModel : PageModel
    {
        private readonly IDispatcher _dispatcher;

        public ListModel(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IList<ProductEntity> Products { get; private set; }

        public async Task OnGetAsync()
        {
            var query = new GetProducts();
            Products = await _dispatcher.GetResultAsync(query);
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var command = new DeleteProduct
            {
                AggregateRootId = id
            };

            await _dispatcher.SendAsync(command);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostPublishAsync(Guid id)
        {
            var command = new PublishProduct
            {
                AggregateRootId = id
            };

            var result = await _dispatcher.SendAsync<bool>(command);

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostWithdrawAsync(Guid id)
        {
            var command = new WithdrawProduct
            {
                AggregateRootId = id
            };

            await _dispatcher.SendAsync(command);

            return RedirectToPage();
        }
    }
}