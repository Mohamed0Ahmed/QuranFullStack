using Microsoft.EntityFrameworkCore;
using QuranDashboard.Domain.Quran.Ayahs;
using QuranDashboard.Domain.Quran.MushafPages;
using QuranDashboard.Domain.Quran.Surahs;
using QuranDashboard.Domain.Quran.Words;

namespace QuranDashboard.Infrastructure.Persistence;

public sealed class QuranDashboardDbContext(DbContextOptions<QuranDashboardDbContext> options) : DbContext(options)
{
    public DbSet<Surah> QuranSurahs => Set<Surah>();
    public DbSet<Ayah> QuranAyahs => Set<Ayah>();
    public DbSet<MushafPage> QuranMushafPages => Set<MushafPage>();
    public DbSet<MushafLine> QuranMushafLines => Set<MushafLine>();
    public DbSet<QuranWord> QuranWords => Set<QuranWord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QuranDashboardDbContext).Assembly);
    }
}
