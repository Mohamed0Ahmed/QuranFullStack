using QuranDashboard.Domain.Linking;

namespace QuranDashboard.Infrastructure.Persistence.Configurations.Linking;

public sealed class LinkingDataStateConfiguration : IEntityTypeConfiguration<LinkingDataState>
{
    public void Configure(EntityTypeBuilder<LinkingDataState> builder)
    {
        builder.ToTable("linking_data_state", table =>
        {
            table.HasCheckConstraint("ck_linking_data_state_singleton", "id = 1");
            table.HasCheckConstraint("ck_linking_data_state_generation", "generation > 0");
        });

        builder.HasKey(state => state.Id);

        builder.Property(state => state.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(state => state.Generation)
            .IsRequired()
            .HasColumnName("generation");

        builder.Property(state => state.UpdatedAtUtc)
            .IsRequired()
            .HasColumnName("updated_at_utc");

        builder.HasData(new LinkingDataState
        {
            Id = 1,
            Generation = 1,
            UpdatedAtUtc = DateTimeOffset.UnixEpoch,
        });
    }
}
