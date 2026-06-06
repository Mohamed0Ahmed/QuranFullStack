using Microsoft.EntityFrameworkCore;

namespace QuranDashboard.Infrastructure.Persistence;

public sealed class QuranDashboardDbContext(DbContextOptions<QuranDashboardDbContext> options) : DbContext(options)
{
}
