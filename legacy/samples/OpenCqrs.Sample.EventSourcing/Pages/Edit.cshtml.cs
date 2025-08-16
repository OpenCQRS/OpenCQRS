using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenCqrs.Sample.EventSourcing.Domain.Commands;
using OpenCqrs.Sample.EventSourcing.Reporting.Data;
using OpenCqrs.Sample.EventSourcing.Reporting.Queries;
using OpenCqrs.UI.Models;
using OpenCqrs.UI.Queries;

namespace OpenCqrs.Sample.EventSourcing.Pages
{
    public class EditModel : PageModel
    {
        private readonly IDispatcher _dispatcher;

        public EditModel(IDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        [BindProperty]
        public ProductEntity Product { get; set; }

        public AggregateModel AggregateModel { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Product = await _dispatcher.GetResultAsync(new GetProduct
            {
                ProductId = id
            });

            AggregateModel = await _dispatcher.GetResultAsync(new GetAggregateModel
            {
                AggregateRootId = id
            });

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            var command = new UpdateProduct
            {
                AggregateRootId = id,
                Name = Product.Name,
                Description = Product.Description,
                Price = Product.Price
            };

            await _dispatcher.SendAsync(command);

            return RedirectToPage("./Edit", new { id });
        }
    }
}