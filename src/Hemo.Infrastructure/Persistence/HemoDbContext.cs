using Microsoft.EntityFrameworkCore;

namespace Hemo.Infrastructure.Persistence;

/// <summary>
/// Stage 1 connection scaffold only; no clinical entity mappings yet.
/// Do not run EnsureCreated or generate an empty initial migration.
/// </summary>
public sealed class HemoDbContext(DbContextOptions<HemoDbContext> options) : DbContext(options) { }
