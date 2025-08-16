using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCqrs.Sample.NoEventSourcing.Domain;
using OpenCqrs.Sample.NoEventSourcing.Domain.Commands;
using OpenCqrs.Sample.NoEventSourcing.Reporting;

namespace OpenCqrs.Sample.NoEventSourcing.Pages
{
    public class ListModel : PageModel
    {
        private readonly IDispatcher _dispatcher;

        public ListModel(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public IList<Product> Products { get; private set; }

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

            await _dispatcher.SendAsync(command);

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