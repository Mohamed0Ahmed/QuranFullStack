using System.Net.Http.Headers;
using System.Net.Http.Json;
using QuranDashboard.Application.Abstractions.Access;
using QuranDashboard.Application.Abstractions.Security;
using QuranDashboard.Application.Abstractions.Security.Permissions;
using QuranDashboard.Domain.Access;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Api.Access;

[Collection(nameof(AccessCollection))]
public sealed class AccessAdministrationEndpointTests(AccessTestFixture fixture)
{
    [Fact]
    public async Task Accept_PersistsTheTransitionInitialGrantsAndOrderedAuditTrail()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-accept-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Pending);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        var requestedCodes = new[]
        {
            AbwabPermissions.Doors.Archive,
            AbwabPermissions.Sections.Create,
        };
        using var client = CreateOwnerClient();

        using var response = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/accept",
            new
            {
                expectedVersion = target.Version,
                permissionCodes = requestedCodes,
                reason = "Accept the verified research editor.",
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("status").GetString().Should().Be("active");
        envelope.GetProperty("data").GetProperty("permissionCodes").EnumerateArray()
            .Select(code => code.GetString()).Should().Equal(CanonicalCodes(requestedCodes));

        var persisted = (await fixture.GetUserBySubAsync(targetSub))!;
        persisted.Status.Should().Be(UserStatus.Active);
        (await GetGrantCodesAsync(targetId)).Should().Equal(CanonicalCodes(requestedCodes));

        var audit = await GetAuditEventsAsync(targetId);
        audit.Select(eventItem => eventItem.ActionType).Should().Equal(
            AccessAuditActionType.UserAccepted,
            AccessAuditActionType.UserActivated,
            AccessAuditActionType.PermissionGranted,
            AccessAuditActionType.PermissionGranted);
        audit.Select(eventItem => eventItem.PermissionCode).Should().Equal(
            null,
            null,
            CanonicalCodes(requestedCodes)[0],
            CanonicalCodes(requestedCodes)[1]);
        audit.Should().OnlyContain(eventItem =>
            eventItem.ActorType == AccessAuditActorType.User
            && eventItem.ActorUserId == ownerId
            && eventItem.Metadata.SchemaVersion == 1
            && eventItem.Reason == "Accept the verified research editor.");

        using var targetSnapshot = JsonDocument.Parse(audit[0].TargetSnapshotJson);
        targetSnapshot.RootElement.GetProperty("id").GetInt32().Should().Be(targetId);
        targetSnapshot.RootElement.GetProperty("normalizedEmail").GetString().Should().Be(persisted.NormalizedEmail);
        using var beforeState = JsonDocument.Parse(audit[0].BeforeStateJson!);
        using var afterState = JsonDocument.Parse(audit[0].AfterStateJson!);
        beforeState.RootElement.GetProperty("permissionCodes").GetArrayLength().Should().Be(0);
        afterState.RootElement.GetProperty("permissionCodes").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task DisableThenReactivate_RevokesEveryGrantWithoutRestoringAny()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-disable-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var originalCodes = new[]
        {
            AbwabPermissions.TemplateNodes.Delete,
            AbwabPermissions.Doors.Create,
        };
        await AddGrantsAsync(targetId, ownerId, originalCodes);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        using var client = CreateOwnerClient();

        using var disableResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/disable",
            new { expectedVersion = target.Version, reason = "Suspend access during account review." });

        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var disabled = (await fixture.GetUserBySubAsync(targetSub))!;
        disabled.Status.Should().Be(UserStatus.Disabled);
        (await GetGrantCodesAsync(targetId)).Should().BeEmpty();

        var afterDisableAudit = await GetAuditEventsAsync(targetId);
        afterDisableAudit.Select(eventItem => eventItem.ActionType).Should().Equal(
            AccessAuditActionType.PermissionRevoked,
            AccessAuditActionType.PermissionRevoked,
            AccessAuditActionType.UserDisabled);
        afterDisableAudit.Take(2).Select(eventItem => eventItem.PermissionCode)
            .Should().Equal(CanonicalCodes(originalCodes));

        using var reactivateResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/reactivate",
            new { expectedVersion = disabled.Version, reason = "Account review completed." });

        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reactivated = (await fixture.GetUserBySubAsync(targetSub))!;
        reactivated.Status.Should().Be(UserStatus.Active);
        (await GetGrantCodesAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventsAsync(targetId)).Select(eventItem => eventItem.ActionType).Should().Equal(
            AccessAuditActionType.PermissionRevoked,
            AccessAuditActionType.PermissionRevoked,
            AccessAuditActionType.UserDisabled,
            AccessAuditActionType.UserReactivated);
    }

    [Fact]
    public async Task ReplacePermissions_UsesVersionedDeltasAndLeavesIdenticalSetsUnaudited()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-replace-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        await AddGrantsAsync(targetId, ownerId, [AbwabPermissions.Sections.Edit, AbwabPermissions.Doors.Create]);
        var original = (await fixture.GetUserBySubAsync(targetSub))!;
        var desiredCodes = new[]
        {
            AbwabPermissions.Doors.Archive,
            AbwabPermissions.Doors.Create,
        };
        using var client = CreateOwnerClient();

        using var replaceResponse = await client.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = original.Version,
                permissionCodes = desiredCodes,
                reason = "Align current responsibilities with the review assignment.",
            });

        replaceResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetGrantCodesAsync(targetId)).Should().Equal(CanonicalCodes(desiredCodes));
        var changed = (await fixture.GetUserBySubAsync(targetSub))!;
        var auditCount = (await GetAuditEventsAsync(targetId)).Count;
        (await GetAuditEventsAsync(targetId)).Select(eventItem => eventItem.ActionType).Should().Equal(
            AccessAuditActionType.PermissionRevoked,
            AccessAuditActionType.PermissionGranted);

        using var identicalResponse = await client.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = changed.Version,
                permissionCodes = desiredCodes.Reverse().ToArray(),
                reason = "Confirm the same assignment.",
            });

        identicalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        (await fixture.GetUserBySubAsync(targetSub))!.Version.Should().Be(changed.Version);
        (await GetAuditEventsAsync(targetId)).Count.Should().Be(auditCount);

        using var staleResponse = await client.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = original.Version,
                permissionCodes = Array.Empty<string>(),
                reason = "Attempt an outdated replacement.",
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            staleResponse,
            HttpStatusCode.Conflict,
            ApiMessages.AccessAdministrationStaleVersion);
        (await GetGrantCodesAsync(targetId)).Should().Equal(CanonicalCodes(desiredCodes));
        (await GetAuditEventsAsync(targetId)).Count.Should().Be(auditCount);
    }

    [Fact]
    public async Task ConcurrentOwners_CompetingStatusAndGrantMutations_CommitOneOutcomeAndRejectTheOtherAsStale()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var firstOwnerId = await SeedActiveOwnerAsync();
        var secondOwnerId = await SeedActiveOwnerAsync(
            AccessTestFixture.SecondOwnerSub,
            AccessTestFixture.SecondOwnerEmail);
        const string targetSub = "access-admin-concurrent-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        await using var gateConnection = new NpgsqlConnection(fixture.ConnectionString);
        await gateConnection.OpenAsync();
        await using var gateTransaction = await gateConnection.BeginTransactionAsync();
        await using (var gateCommand = new NpgsqlCommand(
            "SELECT id FROM users WHERE id = @target_id FOR UPDATE;",
            gateConnection,
            gateTransaction))
        {
            gateCommand.Parameters.AddWithValue("target_id", targetId);
            await gateCommand.ExecuteScalarAsync();
        }

        await using var firstScope = fixture.ApiServices.CreateAsyncScope();
        await using var secondScope = fixture.ApiServices.CreateAsyncScope();

        var disableTask = firstScope.ServiceProvider.GetRequiredService<IAccessUserMutationService>()
            .DisableAsync(
                AccessTestFixture.OwnerSub,
                new DisableAccessUserCommand(
                    targetId,
                    target.Version,
                    "Disable the account from the first Owner."),
                CancellationToken.None);
        var replaceTask = secondScope.ServiceProvider.GetRequiredService<IAccessUserMutationService>()
            .ReplacePermissionsAsync(
                AccessTestFixture.SecondOwnerSub,
                new ReplaceUserPermissionsCommand(
                    targetId,
                    target.Version,
                    [AbwabPermissions.Doors.Create],
                    "Grant access from the second Owner."),
                CancellationToken.None);

        var observedWaiters = 0;
        try
        {
            observedWaiters = await WaitForAccessMutationLockWaitersAsync(2);
        }
        finally
        {
            await gateTransaction.CommitAsync();
        }

        await Task.WhenAll(disableTask, replaceTask);
        var disableResult = await disableTask;
        var replaceResult = await replaceTask;

        observedWaiters.Should().BeGreaterThanOrEqualTo(2);
        new[] { disableResult.IsSuccess, replaceResult.IsSuccess }.Count(success => success)
            .Should().Be(1);
        new[] { disableResult.Failure, replaceResult.Failure }
            .Count(failure => failure == AccessOperationFailure.StaleVersion)
            .Should().Be(1);

        var persisted = (await fixture.GetUserBySubAsync(targetSub))!;
        var audit = await GetAuditEventsAsync(targetId);
        if (disableResult.IsSuccess)
        {
            persisted.Status.Should().Be(UserStatus.Disabled);
            (await GetGrantCodesAsync(targetId)).Should().BeEmpty();
            audit.Select(eventItem => eventItem.ActionType).Should().Equal(AccessAuditActionType.UserDisabled);
            audit.Should().OnlyContain(eventItem => eventItem.ActorUserId == firstOwnerId);
        }
        else
        {
            persisted.Status.Should().Be(UserStatus.Active);
            (await GetGrantCodesAsync(targetId)).Should().Equal(AbwabPermissions.Doors.Create);
            audit.Select(eventItem => eventItem.ActionType).Should().Equal(AccessAuditActionType.PermissionGranted);
            audit.Should().OnlyContain(eventItem => eventItem.ActorUserId == secondOwnerId);
        }
    }

    [Fact]
    public async Task Mutation_ActorDeownedWhileWaitingForItsTransactionLock_IsRejectedWithoutTargetOrAuditChanges()
    {
        await fixture.ResetAsync();
        var actorId = await SeedActiveOwnerAsync();
        await SeedActiveOwnerAsync(
            AccessTestFixture.SecondOwnerSub,
            AccessTestFixture.SecondOwnerEmail);
        const string targetSub = "access-admin-deowned-actor-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var deownershipTransaction = await connection.BeginTransactionAsync();
        await using (var command = new NpgsqlCommand(
            "UPDATE users SET role_id = NULL, updated_at = now() WHERE id = @actor_id;",
            connection,
            deownershipTransaction))
        {
            command.Parameters.AddWithValue("actor_id", actorId);
            await command.ExecuteNonQueryAsync();
        }

        await using var mutationScope = fixture.ApiServices.CreateAsyncScope();
        var mutation = mutationScope.ServiceProvider.GetRequiredService<IAccessUserMutationService>()
            .DisableAsync(
                AccessTestFixture.OwnerSub,
                new DisableAccessUserCommand(
                    targetId,
                    target.Version,
                    "This mutation must lose authority before its transaction proceeds."),
                CancellationToken.None);

        var observedWaiters = 0;
        try
        {
            observedWaiters = await WaitForAccessMutationLockWaitersAsync(1);
        }
        finally
        {
            await deownershipTransaction.CommitAsync();
        }

        var result = await mutation;

        observedWaiters.Should().BeGreaterThanOrEqualTo(1);
        result.Failure.Should().Be(AccessOperationFailure.ActorNoLongerOwner);
        var persisted = (await fixture.GetUserBySubAsync(targetSub))!;
        persisted.Status.Should().Be(UserStatus.Active);
        persisted.Version.Should().Be(target.Version);
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task ReplacePermissions_RejectsUnknownRetiredDuplicateAndGroupLikeCodes()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-invalid-codes-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Active);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        using var client = CreateOwnerClient();
        IReadOnlyList<string>[] invalidCodeSets =
        [
            ["unknown.permission"],
            ["abwab.doors"],
            [AbwabPermissions.Doors.Create, AbwabPermissions.Doors.Create],
        ];

        foreach (var permissionCodes in invalidCodeSets)
        {
            using var response = await client.PutAsJsonAsync(
                $"/api/access/users/{targetId}/permissions",
                new
                {
                    expectedVersion = target.Version,
                    permissionCodes,
                    reason = "Reject invalid permission input.",
                });

            await ApiEnvelope.AssertFailureEnvelopeAsync(
                response,
                HttpStatusCode.BadRequest,
                ApiMessages.AccessAdministrationInvalidPermissionCodes);
        }

        await RetirePermissionAsync(AbwabPermissions.Doors.Create);

        using var retiredResponse = await client.PutAsJsonAsync(
            $"/api/access/users/{targetId}/permissions",
            new
            {
                expectedVersion = target.Version,
                permissionCodes = new[] { AbwabPermissions.Doors.Create },
                reason = "Reject a retired permission.",
            });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            retiredResponse,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationInvalidPermissionCodes);
        (await GetGrantCodesAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
    }

    [Fact]
    public async Task AuditInsertionFailure_RollsBackTheUserTransitionAndAllAuditRows()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-audit-failure-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Pending);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        using var client = CreateOwnerClient();
        await AddAuditFailureConstraintAsync();

        try
        {
            using var response = await client.PostAsJsonAsync(
                $"/api/access/users/{targetId}/accept",
                new
                {
                    expectedVersion = target.Version,
                    permissionCodes = new[] { AbwabPermissions.Doors.Create },
                    reason = "This audit write must fail atomically.",
                });

            await ApiEnvelope.AssertFailureEnvelopeAsync(
                response,
                HttpStatusCode.InternalServerError,
                ApiMessages.UnexpectedError);
        }
        finally
        {
            await DropAuditFailureConstraintAsync();
        }

        var persisted = (await fixture.GetUserBySubAsync(targetSub))!;
        persisted.Status.Should().Be(UserStatus.Pending);
        persisted.RoleId.Should().BeNull();
        persisted.LogtoSub.Should().Be(targetSub);
        persisted.Version.Should().Be(target.Version);
        (await GetGrantCodesAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventsAsync(targetId)).Should().BeEmpty();
        (await GetAuditEventCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ListUsers_WithMaximumAcceptedPagingValues_ReturnsAnEmptyPage()
    {
        await fixture.ResetAsync();
        await SeedActiveOwnerAsync();
        using var client = CreateOwnerClient();

        using var response = await client.GetAsync($"/api/access/users?page={int.MaxValue}&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ApiEnvelope.ReadDataAsync(response);
        page.GetProperty("page").GetInt32().Should().Be(int.MaxValue);
        page.GetProperty("pageSize").GetInt32().Should().Be(100);
        page.GetProperty("totalCount").GetInt32().Should().Be(1);
        page.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListUsers_WithAnExactMultipleOfFilteredResults_TraversesEveryUserAndReturnsAnEmptyPagePastTheEnd()
    {
        await fixture.ResetAsync();
        await SeedActiveOwnerAsync();
        var userIds = new[]
        {
            await SeedUserAsync("access-admin-page-1", UserStatus.Pending),
            await SeedUserAsync("access-admin-page-2", UserStatus.Pending),
            await SeedUserAsync("access-admin-page-3", UserStatus.Pending),
            await SeedUserAsync("access-admin-page-4", UserStatus.Pending),
        };
        using var client = CreateOwnerClient();

        using var firstPageResponse = await client.GetAsync(
            "/api/access/users?status=pending&isOwner=false&page=1&pageSize=2");
        firstPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPage = await ApiEnvelope.ReadDataAsync(firstPageResponse);

        using var lastPageResponse = await client.GetAsync(
            "/api/access/users?status=pending&isOwner=false&page=2&pageSize=2");
        lastPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lastPage = await ApiEnvelope.ReadDataAsync(lastPageResponse);
        lastPage.GetProperty("page").GetInt32().Should().Be(2);
        lastPage.GetProperty("pageSize").GetInt32().Should().Be(2);
        lastPage.GetProperty("totalCount").GetInt32().Should().Be(userIds.Length);
        lastPage.GetProperty("items").GetArrayLength().Should().Be(2);

        firstPage.GetProperty("items").EnumerateArray()
            .Select(user => user.GetProperty("id").GetInt32())
            .Concat(lastPage.GetProperty("items").EnumerateArray().Select(user => user.GetProperty("id").GetInt32()))
            .Should().BeEquivalentTo(userIds);

        using var emptyPageResponse = await client.GetAsync(
            "/api/access/users?status=pending&isOwner=false&page=3&pageSize=2");
        emptyPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var emptyPage = await ApiEnvelope.ReadDataAsync(emptyPageResponse);
        emptyPage.GetProperty("page").GetInt32().Should().Be(3);
        emptyPage.GetProperty("pageSize").GetInt32().Should().Be(2);
        emptyPage.GetProperty("totalCount").GetInt32().Should().Be(userIds.Length);
        emptyPage.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task OwnerReads_ExposeBoundedProjectionsAndRejectOrdinaryOwnerMutation()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-read-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Pending, displayName: "باحث اختبار");
        using var client = CreateOwnerClient();

        using var ownerMutationResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{ownerId}/disable",
            new { expectedVersion = (await fixture.GetUserBySubAsync(AccessTestFixture.OwnerSub))!.Version, reason = "Forbidden owner mutation." });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            ownerMutationResponse,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationOwnerTargetForbidden);
        (await GetAuditEventsAsync(ownerId)).Should().BeEmpty();

        var normalizedSearch = Uri.EscapeDataString($" {targetSub.ToUpperInvariant()}@EXAMPLE.TEST ");
        using var listResponse = await client.GetAsync(
            $"/api/access/users?status=pending&isOwner=false&search={normalizedSearch}&page=1&pageSize=25");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var listData = await ApiEnvelope.ReadDataAsync(listResponse);
        listData.GetProperty("totalCount").GetInt32().Should().Be(1);
        listData.GetProperty("items")[0].GetProperty("id").GetInt32().Should().Be(targetId);
        listData.GetProperty("items")[0].GetProperty("permissionCount").GetInt32().Should().Be(0);

        using var detailResponse = await client.GetAsync($"/api/access/users/{targetId}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ApiEnvelope.ReadDataAsync(detailResponse);
        detail.GetProperty("sub").GetString().Should().Be(targetSub);
        detail.GetProperty("displayName").GetString().Should().Be("باحث اختبار");

        using var ownerPermissionsResponse = await client.GetAsync($"/api/access/users/{ownerId}/permissions");
        ownerPermissionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var ownerPermissions = await ApiEnvelope.ReadDataAsync(ownerPermissionsResponse);
        ownerPermissions.GetProperty("isOwner").GetBoolean().Should().BeTrue();
        ownerPermissions.GetProperty("permissionCodes").GetArrayLength().Should().Be(0);

        using var catalogueResponse = await client.GetAsync("/api/access/permissions");
        catalogueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalogue = await ApiEnvelope.ReadDataAsync(catalogueResponse);
        catalogue.GetArrayLength().Should().Be(AbwabPermissionCatalogue.All.Count);
        catalogue[0].GetProperty("code").GetString().Should().Be(AbwabPermissionCatalogue.All[0].Code);
        catalogue[0].GetProperty("groupKey").GetString().Should().NotBeNullOrWhiteSpace();

        using var statusResponse = await client.GetAsync("/api/access/owner-reconciliation/status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = await ApiEnvelope.ReadDataAsync(statusResponse);
        var candidates = status.GetProperty("candidates").EnumerateArray().ToArray();
        candidates.Should().Contain(candidate =>
            candidate.GetProperty("userId").ValueKind == JsonValueKind.Number
            && candidate.GetProperty("userId").GetInt32() == ownerId
            && candidate.GetProperty("state").GetString() == "Unchanged");
        candidates.Should().Contain(candidate =>
            candidate.GetProperty("userId").ValueKind == JsonValueKind.Null
            && candidate.GetProperty("state").GetString() == "AwaitingVerifiedSignIn");
    }

    [Fact]
    public async Task AuditEvents_UseKeysetOrderingAndAllDocumentedFilters()
    {
        await fixture.ResetAsync();
        await SynchronizePermissionsAsync();
        var ownerId = await SeedActiveOwnerAsync();
        const string targetSub = "access-admin-audit-query-target";
        var targetId = await SeedUserAsync(targetSub, UserStatus.Pending);
        var target = (await fixture.GetUserBySubAsync(targetSub))!;
        using var client = CreateOwnerClient();
        var fromUtc = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddMinutes(-1).ToString("O"));

        using var acceptResponse = await client.PostAsJsonAsync(
            $"/api/access/users/{targetId}/accept",
            new
            {
                expectedVersion = target.Version,
                permissionCodes = new[] { AbwabPermissions.Doors.Create, AbwabPermissions.Doors.Archive },
                reason = "Create audit records for retrieval.",
            });
        acceptResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var firstPageResponse = await client.GetAsync($"/api/access/audit-events?targetUserId={targetId}&pageSize=1");
        firstPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstPage = await ApiEnvelope.ReadDataAsync(firstPageResponse);
        var firstId = firstPage.GetProperty("items")[0].GetProperty("id").GetInt64();
        var nextCursor = firstPage.GetProperty("nextCursor").GetString();
        nextCursor.Should().NotBeNullOrWhiteSpace();

        using var secondPageResponse = await client.GetAsync(
            $"/api/access/audit-events?targetUserId={targetId}&pageSize=1&cursor={Uri.EscapeDataString(nextCursor!)}");
        secondPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondPage = await ApiEnvelope.ReadDataAsync(secondPageResponse);
        secondPage.GetProperty("items")[0].GetProperty("id").GetInt64().Should().BeLessThan(firstId);

        var permissionCode = Uri.EscapeDataString(AbwabPermissions.Doors.Archive);
        using var filteredResponse = await client.GetAsync(
            $"/api/access/audit-events?targetUserId={targetId}&actorUserId={ownerId}&actionType=PermissionGranted&permissionCode={permissionCode}&fromUtc={fromUtc}&toUtc={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddMinutes(1).ToString("O"))}&pageSize=25");
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var filtered = await ApiEnvelope.ReadDataAsync(filteredResponse);
        filtered.GetProperty("items").GetArrayLength().Should().Be(1);
        var eventItem = filtered.GetProperty("items")[0];
        eventItem.GetProperty("permissionCode").GetString().Should().Be(AbwabPermissions.Doors.Archive);
        eventItem.GetProperty("actorSnapshot").GetProperty("id").GetInt32().Should().Be(ownerId);
        eventItem.GetProperty("targetSnapshot").GetProperty("id").GetInt32().Should().Be(targetId);
        eventItem.GetProperty("metadata").GetProperty("schemaVersion").GetInt32().Should().Be(1);

        using var oversizedResponse = await client.GetAsync("/api/access/audit-events?pageSize=101");
        await ApiEnvelope.AssertFailureEnvelopeAsync(
            oversizedResponse,
            HttpStatusCode.BadRequest,
            ApiMessages.AccessAdministrationInvalidRequest);
    }

    private Task<int> SeedActiveOwnerAsync()
    {
        return SeedActiveOwnerAsync(AccessTestFixture.OwnerSub, AccessTestFixture.OwnerEmail);
    }

    private async Task<int> SeedActiveOwnerAsync(string sub, string email)
    {
        var ownerRoleId = (await fixture.GetRolesAsync()).Single(role => role.Name == RoleNames.Owner).Id;
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = email,
            RoleId = ownerRoleId,
            Status = UserStatus.Active,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private HttpClient CreateOwnerClient()
    {
        var client = fixture.CreateApiClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            TestJwtTokens.Mint(AccessTestFixture.OwnerSub));
        return client;
    }

    private async Task<int> SeedUserAsync(string sub, UserStatus status, string? displayName = null)
    {
        return await fixture.InsertUserAsync(new User
        {
            LogtoSub = sub,
            Email = $"{sub}@example.test",
            DisplayName = displayName,
            Status = status,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        });
    }

    private async Task SynchronizePermissionsAsync()
    {
        await using var scope = fixture.ApiServices.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IPermissionCatalogueSynchronizer>()
            .SynchronizeAsync(CancellationToken.None);
    }

    private async Task AddGrantsAsync(int targetUserId, int actorUserId, IReadOnlyList<string> permissionCodes)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var permissionIds = await db.AccessPermissions
            .Where(permission => permissionCodes.Contains(permission.Code))
            .ToDictionaryAsync(permission => permission.Code, permission => permission.Id);
        foreach (var permissionCode in permissionCodes)
        {
            db.AccessUserPermissions.Add(new UserPermission
            {
                UserId = targetUserId,
                PermissionId = permissionIds[permissionCode],
                GrantedByUserId = actorUserId,
                GrantedAtUtc = DateTimeOffset.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<IReadOnlyList<string>> GetGrantCodesAsync(int targetUserId)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessUserPermissions.AsNoTracking()
            .Where(grant => grant.UserId == targetUserId)
            .OrderBy(grant => grant.Permission.DisplayOrder)
            .Select(grant => grant.Permission.Code)
            .ToListAsync();
    }

    private async Task RetirePermissionAsync(string permissionCode)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE permissions SET retired_at = {DateTimeOffset.UtcNow} WHERE code = {permissionCode};");
    }

    private async Task<int> WaitForAccessMutationLockWaitersAsync(int expectedCount)
    {
        await using var observerConnection = new NpgsqlConnection(fixture.ConnectionString);
        await observerConnection.OpenAsync();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        var highestObservedCount = 0;
        do
        {
            await using var command = new NpgsqlCommand(
                """
                SELECT count(*)::integer
                FROM pg_stat_activity
                WHERE datname = current_database()
                  AND pid <> pg_backend_pid()
                  AND state = 'active'
                  AND wait_event_type = 'Lock'
                  AND query LIKE '%SELECT id FROM users WHERE logto_sub =%'
                  AND query LIKE '%ORDER BY id FOR UPDATE%'
                """,
                observerConnection);
            highestObservedCount = Math.Max(
                highestObservedCount,
                Convert.ToInt32(await command.ExecuteScalarAsync()));
            if (highestObservedCount >= expectedCount)
            {
                return highestObservedCount;
            }

            await Task.Delay(25);
        }
        while (DateTimeOffset.UtcNow < deadline);

        return highestObservedCount;
    }

    private async Task<IReadOnlyList<AccessAuditEvent>> GetAuditEventsAsync(int targetUserId)
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessAuditEvents.AsNoTracking()
            .Where(eventItem => eventItem.TargetUserId == targetUserId)
            .OrderBy(eventItem => eventItem.Id)
            .ToListAsync();
    }

    private async Task<int> GetAuditEventCountAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AccessAuditEvents.CountAsync();
    }

    private async Task AddAuditFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """
            ALTER TABLE access_audit_events
            ADD CONSTRAINT ck_test_reject_user_accepted_audit
            CHECK (action_type <> 'UserAccepted');
            """);
    }

    private async Task DropAuditFailureConstraintAsync()
    {
        await using var scope = fixture.QueryServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "ALTER TABLE access_audit_events DROP CONSTRAINT ck_test_reject_user_accepted_audit;");
    }

    private static IReadOnlyList<string> CanonicalCodes(IEnumerable<string> codes) => AbwabPermissionCatalogue.All
        .Where(definition => codes.Contains(definition.Code, StringComparer.Ordinal))
        .OrderBy(definition => definition.DisplayOrder)
        .Select(definition => definition.Code)
        .ToArray();

}
