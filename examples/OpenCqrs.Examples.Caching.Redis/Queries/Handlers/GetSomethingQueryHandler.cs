﻿using OpenCqrs.Queries;
using OpenCqrs.Results;

namespace OpenCqrs.Examples.Caching.Redis.Queries.Handlers;

public class GetSomethingQueryHandler : IQueryHandler<GetSomethingQuery, string>
{
    public async Task<Result<string>> Handle(GetSomethingQuery query, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return "Hello from GetSomethingQueryHandler";
    }
}
