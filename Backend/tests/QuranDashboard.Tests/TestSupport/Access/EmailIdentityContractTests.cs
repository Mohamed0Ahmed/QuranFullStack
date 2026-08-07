using QuranDashboard.Domain.Access;

namespace QuranDashboard.Tests.TestSupport.Access;

public sealed class EmailIdentityContractTests
{
    [Fact]
    public void ReadOnlyCollisionScan_ReportsEveryCandidateWithoutChoosingAWinner()
    {
        var users = new[]
        {
            new User { Id = 11, Email = "owner@example.test" },
            new User { Id = 12, Email = " Owner@Example.Test " },
            new User { Id = 13, Email = "teacher@example.test" },
        };

        var collisions = CurrentUserCollisionScan.Find(
            users,
            email => email.Trim().ToUpperInvariant());

        collisions.Should().ContainSingle();
        collisions[0].NormalizedEmail.Should().Be("OWNER@EXAMPLE.TEST");
        collisions[0].Users.Select(user => user.Id).Should().Equal(11, 12);
    }
}
