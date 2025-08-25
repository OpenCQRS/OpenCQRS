using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenCqrs.EventSourcing.Store.EntityFrameworkCore;

namespace OpenCqrs.Examples.EventSourcing.EntityFrameworkCore.Data;

public sealed class MyStoreDbContext(
    DbContextOptions<DomainDbContext> options,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor)
    : DomainDbContext(options, timeProvider, httpContextAccessor);
