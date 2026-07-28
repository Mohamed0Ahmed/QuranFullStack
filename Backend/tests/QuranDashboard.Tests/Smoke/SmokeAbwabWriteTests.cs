using System.Net.Http.Json;
using System.Text;
using QuranDashboard.Tests.TestSupport.Http;

namespace QuranDashboard.Tests.Smoke;

// Dedicated coverage for the write routes SmokeRoutePipelineTests deliberately skips (ParityOnly) —
// see SmokeRouteCatalog. Every test resets Abwab tables at the START, not just cleanup: a test that
// fails mid-way would otherwise poison whichever test runs next, which is what actually makes the
// "run the smoke filter twice" verification meaningful.
[Collection(nameof(SmokeCollection))]
public sealed class SmokeAbwabWriteTests(SmokeApiFixture fixture)
{
    [Fact]
    public async Task CreateSection_WithValidName_ReturnsCreatedWithEnvelope()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "القسم الأول" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("name").GetString().Should().Be("القسم الأول");
    }

    [Theory]
    [InlineData("{\"name\": null}")]
    [InlineData("{}")]
    public async Task CreateSection_WithNullOrMissingName_ReturnsBadRequestInFailureEnvelope(string body)
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/abwab/sections", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task CreateSection_WithBlankName_ReturnsBadRequest()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "   " });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.BadRequest, ApiMessages.AbwabSectionInvalidName);
    }

    [Fact]
    public async Task CreateSection_WithDuplicateName_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var first = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "أبواب العقيدة" });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        using var second = await client.PostAsJsonAsync("/api/abwab/sections", new { name = "أبواب العقيدة" });

        await ApiEnvelope.AssertFailureEnvelopeAsync(second, HttpStatusCode.Conflict, ApiMessages.AbwabSectionDuplicateName);
    }

    [Fact]
    public async Task RenameSection_WithCorrectVersion_ReturnsOkWithUpdatedName()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateSectionAsync(client, "قسم قابل لإعادة التسمية");

        using var response = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "اسم جديد", version });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("name").GetString().Should().Be("اسم جديد");
    }

    [Fact]
    public async Task RenameSection_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PutAsJsonAsync("/api/abwab/sections/999999", new { name = "لا يوجد", version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabSectionNotFound);
    }

    [Fact]
    public async Task RenameSection_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, staleVersion) = await CreateSectionAsync(client, "قسم للتحديث المتزامن");
        using var firstRename = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "تحديث أول", version = staleVersion });
        firstRename.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "تحديث ثانٍ", version = staleVersion });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabSectionStaleVersion);
    }

    [Fact]
    public async Task RenameSection_ToAnotherSectionsName_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (_, _) = await CreateSectionAsync(client, "الاسم المحجوز");
        var (id, version) = await CreateSectionAsync(client, "اسم آخر");

        using var response = await client.PutAsJsonAsync($"/api/abwab/sections/{id}", new { name = "الاسم المحجوز", version });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabSectionDuplicateName);
    }

    [Fact]
    public async Task RenameSection_WithNullName_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateSectionAsync(client, "قسم لفحص الربط");
        using var content = new StringContent(
            $"{{\"name\": null, \"version\": {version}}}", Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/api/abwab/sections/{id}", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task DeleteSection_WithNoLiveDoors_ReturnsNoContentAndNoBody()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateSectionAsync(client, "قسم فارغ للحذف");

        using var response = await client.DeleteAsync($"/api/abwab/sections/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSection_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.DeleteAsync("/api/abwab/sections/999999");

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabSectionNotFound);
    }

    [Fact]
    public async Task DeleteSection_WithLiveDoors_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateSectionAsync(client, "قسم يحوي بابًا حيًا");
        await CreateDoorAsync(client, "باب حي", sectionId: id);

        using var response = await client.DeleteAsync($"/api/abwab/sections/{id}");

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabSectionHasLiveDoors);
    }

    // ---- Doors ----

    [Fact]
    public async Task CreateDoor_WithValidData_ReturnsCreatedWithAliases()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors",
            new { name = "باب الإيمان", aliases = new[] { "إيمان", "عقيدة" } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        var data = envelope.GetProperty("data");
        data.GetProperty("name").GetString().Should().Be("باب الإيمان");
        data.GetProperty("aliases").EnumerateArray().Select(a => a.GetString())
            .Should().BeEquivalentTo(["إيمان", "عقيدة"]);
    }

    [Fact]
    public async Task CreateDoor_WithNullName_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();
        using var content = new StringContent("{\"name\": null}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/abwab/doors", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task CreateDoor_WithUnknownParent_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors", new { name = "باب يتيم", parentId = 999999 });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorParentNotFound);
    }

    // A real id that exists but is archived. The 999999 case above cannot tell "missing" from "archived":
    // every parent lookup filters on DeletedAtUtc == null, and a query that dropped that filter would
    // still pass it. Nesting under an archived parent would author a live door into a dead subtree.
    [Fact]
    public async Task CreateDoor_UnderAnArchivedParent_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentId, parentVersion) = await CreateDoorAsync(client, "أب يُؤرشف قبل إضافة ابنه");
        using var archiveResponse = await SendWithBodyAsync(
            client, HttpMethod.Delete, $"/api/abwab/doors/{parentId}", new { version = parentVersion });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var response = await client.PostAsJsonAsync("/api/abwab/doors",
            new { parentId, name = "ابن تحت أب مؤرشف", aliases = Array.Empty<string>() });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorParentNotFound);
    }

    [Fact]
    public async Task CreateDoor_WithUnknownSection_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors", new { name = "باب بلا قسم", sectionId = 999999 });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorSectionNotFound);
    }

    [Fact]
    public async Task CreateDoor_UnderAParentInAnotherSection_ReturnsBadRequest()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentSectionId, _) = await CreateSectionAsync(client, "قسم الأب");
        var (otherSectionId, _) = await CreateSectionAsync(client, "قسم مخالف");
        var (parentId, _) = await CreateDoorAsync(client, "أب في قسمه", sectionId: parentSectionId);

        using var response = await client.PostAsJsonAsync("/api/abwab/doors",
            new { sectionId = otherSectionId, parentId, name = "ابن بقسم مخالف", aliases = Array.Empty<string>() });

        await ApiEnvelope.AssertFailureEnvelopeAsync(
            response, HttpStatusCode.BadRequest, ApiMessages.AbwabDoorSectionParentMismatch);
    }

    // The other half of the create rule: an omitted section under a parent derives, it does not write null.
    [Fact]
    public async Task CreateDoor_UnderAParentWithNoSectionStated_InheritsTheParentsSection()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم يُورَّث للابن");
        var (parentId, _) = await CreateDoorAsync(client, "أب يورّث قسمه", sectionId: sectionId);

        using var response = await client.PostAsJsonAsync("/api/abwab/doors",
            new { parentId, name = "ابن بلا قسم مذكور", aliases = Array.Empty<string>() });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("sectionId").GetInt32().Should().Be(sectionId);
    }

    [Fact]
    public async Task CreateDoor_WithDuplicateNameAtRoot_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        await CreateDoorAsync(client, "باب مكرر");

        using var response = await client.PostAsJsonAsync("/api/abwab/doors", new { name = "باب مكرر" });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorDuplicateName);
    }

    [Fact]
    public async Task EditDoor_RoundTripsVersionAcrossTwoConsecutiveEdits()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateDoorAsync(client, "باب قابل للتعديل");

        using var firstEdit = await client.PutAsJsonAsync($"/api/abwab/doors/{id}",
            new { name = "تعديل أول", description = (string?)null, representativeAyahText = (string?)null, aliases = Array.Empty<string>(), version });
        firstEdit.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstData = await ApiEnvelope.ReadDataAsync(firstEdit);
        var versionAfterFirstEdit = firstData.GetProperty("version").GetUInt32();

        // Proves the version returned by a write is itself usable for the NEXT write — if Npgsql/EF
        // weren't round-tripping the post-update xmin correctly, this second edit would 409 instead.
        using var secondEdit = await client.PutAsJsonAsync($"/api/abwab/doors/{id}",
            new { name = "تعديل ثانٍ", description = (string?)null, representativeAyahText = (string?)null, aliases = Array.Empty<string>(), version = versionAfterFirstEdit });

        secondEdit.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EditDoor_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PutAsJsonAsync("/api/abwab/doors/999999",
            new { name = "لا يوجد", description = (string?)null, representativeAyahText = (string?)null, aliases = Array.Empty<string>(), version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task EditDoor_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, staleVersion) = await CreateDoorAsync(client, "باب لتعديل متزامن");
        using var firstEdit = await client.PutAsJsonAsync($"/api/abwab/doors/{id}",
            new { name = "تعديل أول", aliases = Array.Empty<string>(), version = staleVersion });
        firstEdit.StatusCode.Should().Be(HttpStatusCode.OK);

        using var response = await client.PutAsJsonAsync($"/api/abwab/doors/{id}",
            new { name = "تعديل ثانٍ", aliases = Array.Empty<string>(), version = staleVersion });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
    }

    [Fact]
    public async Task EditDoor_WithNullName_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateDoorAsync(client, "باب لفحص الربط");
        using var content = new StringContent(
            $"{{\"name\": null, \"version\": {version}}}", Encoding.UTF8, "application/json");

        using var response = await client.PutAsync($"/api/abwab/doors/{id}", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task EditDoor_ReplacesAliasesWholesale()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateDoorAsync(client, "باب له اسماء بديلة", aliases: ["قديم١", "قديم٢"]);

        using var response = await client.PutAsJsonAsync($"/api/abwab/doors/{id}",
            new { name = "باب له اسماء بديلة", description = (string?)null, representativeAyahText = (string?)null, aliases = new[] { "قديم٢", "جديد" }, version });

        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").GetProperty("aliases").EnumerateArray().Select(a => a.GetString())
            .Should().BeEquivalentTo(["قديم٢", "جديد"]);
    }

    [Fact]
    public async Task MoveDoor_UnderAnotherSectionsDoor_InheritsThatSection()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم الوجهة");
        var (targetParentId, _) = await CreateDoorAsync(client, "باب في القسم", sectionId: sectionId);
        var (movedId, movedVersion) = await CreateDoorAsync(client, "باب منقول");

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{movedId}/move",
            new { targetParentId, version = movedVersion });

        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        var data = envelope.GetProperty("data");
        data.GetProperty("parentId").GetInt32().Should().Be(targetParentId);
        data.GetProperty("sectionId").GetInt32().Should().Be(sectionId);
    }

    [Fact]
    public async Task MoveDoor_IntoOwnDescendant_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentId, parentVersion) = await CreateDoorAsync(client, "الباب الأب");
        var (childId, _) = await CreateDoorAsync(client, "الباب الابن", parentId: parentId);

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{parentId}/move",
            new { targetParentId = childId, version = parentVersion });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorWouldCycle);
    }

    [Fact]
    public async Task MoveDoor_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/999999/move",
            new { targetParentId = (int?)null, targetSectionId = (int?)null, version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task MoveDoor_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم لنقل متزامن");
        var (id, _) = await CreateDoorAsync(client, "باب لنقل متزامن");

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{id}/move",
            new { targetSectionId = sectionId, version = 999_999u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
    }

    [Fact]
    public async Task MoveDoor_WithNullVersion_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لفحص ربط النقل");
        using var content = new StringContent(
            "{\"targetSectionId\":null,\"targetParentId\":null,\"version\":null}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync($"/api/abwab/doors/{id}/move", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task ReorderDoor_ToValidPosition_ResequencesSiblings()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (firstId, _) = await CreateDoorAsync(client, "الأول");
        var (secondId, secondVersion) = await CreateDoorAsync(client, "الثاني");
        await CreateDoorAsync(client, "الثالث");

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{secondId}/order",
            new { position = 1, version = secondVersion });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderValues = await GetDoorOrderValuesAsync(null, null);
        orderValues[secondId].Should().Be(1);
        orderValues[firstId].Should().Be(2);
        orderValues.Values.Order().Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ReorderDoor_OutOfRange_ReturnsBadRequest()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateDoorAsync(client, "باب وحيد");

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{id}/order", new { position = 5, version });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.BadRequest, ApiMessages.AbwabDoorInvalidPosition);
    }

    // §6's move row lists "duplicate at target" as its own 409, distinct from stale and cycle: two doors
    // may legally share a name in DIFFERENT scopes, and the collision only exists once one lands in the
    // other's scope. Nothing else in this suite puts the unique index under a move.
    [Fact]
    public async Task MoveDoor_IntoAScopeHoldingThatName_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم يحمل الاسم مسبقًا");
        await CreateDoorAsync(client, "باب الإيمان", sectionId: sectionId);
        var (movedId, movedVersion) = await CreateDoorAsync(client, "باب الإيمان");

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{movedId}/move",
            new { targetSectionId = sectionId, version = movedVersion });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorDuplicateName);
    }

    [Fact]
    public async Task ReorderDoor_WithNullVersion_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لفحص ربط الترتيب");
        using var content = new StringContent("{\"position\":1,\"version\":null}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync($"/api/abwab/doors/{id}/order", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task ReorderDoor_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/999999/order", new { position = 1, version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task ReorderDoor_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لإعادة ترتيب متزامنة");

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{id}/order",
            new { position = 1, version = 999_999u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
    }

    [Fact]
    public async Task BulkMoveDoors_MovesBothDoorsAndResequencesDestination()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (targetSectionId, _) = await CreateSectionAsync(client, "قسم وجهة النقل الجماعي");
        var (existingId, _) = await CreateDoorAsync(client, "باب موجود مسبقًا في الوجهة", sectionId: targetSectionId);
        var (firstId, firstVersion) = await CreateDoorAsync(client, "أول باب لنقل جماعي ناجح");
        var (secondId, secondVersion) = await CreateDoorAsync(client, "ثاني باب لنقل جماعي ناجح");

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-move", new
        {
            doors = new object[]
            {
                new { doorId = firstId, version = firstVersion },
                new { doorId = secondId, version = secondVersion },
            },
            targetSectionId,
            targetParentId = (int?)null,
        });

        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        var movedDtos = envelope.GetProperty("data").EnumerateArray().ToList();
        movedDtos.Should().HaveCount(2);
        movedDtos.Should().OnlyContain(dto => dto.GetProperty("sectionId").GetInt32() == targetSectionId);

        var orderValues = await GetDoorOrderValuesAsync(targetSectionId, null);
        orderValues.Should().ContainKey(existingId).WhoseValue.Should().Be(1);
        orderValues.Values.Order().Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task BulkMoveDoors_WithNullElement_ReturnsBadRequest()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();
        using var content = new StringContent(
            "{\"doors\":[null],\"targetSectionId\":null,\"targetParentId\":null}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/abwab/doors/bulk-move", content);

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.BadRequest, ApiMessages.AbwabDoorsBulkInvalidRequest);
    }

    [Fact]
    public async Task BulkMoveDoors_WithOneStaleVersion_FailsWholeBatch()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (firstId, firstVersion) = await CreateDoorAsync(client, "أول باب دفعي");
        var (secondId, _) = await CreateDoorAsync(client, "ثاني باب دفعي");
        var (targetSectionId, _) = await CreateSectionAsync(client, "قسم الدفعة");
        const uint staleVersion = 999_999;

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-move", new
        {
            doors = new object[] { new { doorId = firstId, version = firstVersion }, new { doorId = secondId, version = staleVersion } },
            targetSectionId,
            targetParentId = (int?)null,
        });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);

        var firstAfter = await GetDoorAsync(firstId);
        firstAfter.SectionId.Should().BeNull("the whole batch must fail — the valid-version door must not have moved either");
    }

    [Fact]
    public async Task BulkMoveDoors_WithUnknownDoorId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-move", new
        {
            doors = new object[] { new { doorId = 999999, version = 0u } },
            targetSectionId = (int?)null,
            targetParentId = (int?)null,
        });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task BulkMoveDoors_IntoAScopeHoldingOneOfTheirNames_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم وجهة يحمل اسمًا مكررًا");
        await CreateDoorAsync(client, "باب التوحيد", sectionId: sectionId);
        var (firstId, firstVersion) = await CreateDoorAsync(client, "باب التوحيد");
        var (secondId, secondVersion) = await CreateDoorAsync(client, "باب لا يتعارض");

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-move", new
        {
            doors = new object[]
            {
                new { doorId = firstId, version = firstVersion },
                new { doorId = secondId, version = secondVersion },
            },
            targetSectionId = sectionId,
            targetParentId = (int?)null,
        });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorDuplicateName);

        // All-or-nothing: the door that would NOT have collided must not have moved either.
        (await GetDoorAsync(secondId)).SectionId.Should().BeNull();
    }

    [Fact]
    public async Task BulkArchiveDoors_ArchivesSubtreeAndResequencesSiblings()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentId, parentVersion) = await CreateDoorAsync(client, "أب للأرشفة الجماعية");
        var (childId, _) = await CreateDoorAsync(client, "ابن للأرشفة الجماعية", parentId: parentId);
        var (siblingId, _) = await CreateDoorAsync(client, "شقيق يبقى حيًا");

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-archive",
            new { doors = new object[] { new { doorId = parentId, version = parentVersion } } });

        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("data").EnumerateArray().Select(e => e.GetInt32())
            .Should().BeEquivalentTo([parentId, childId]);

        var sibling = await GetDoorAsync(siblingId);
        sibling.OrderValue.Should().Be(1);
    }

    [Fact]
    public async Task BulkArchiveDoors_WithNullElement_ReturnsBadRequest()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();
        using var content = new StringContent("{\"doors\":[null]}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/abwab/doors/bulk-archive", content);

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.BadRequest, ApiMessages.AbwabDoorsBulkInvalidRequest);
    }

    [Fact]
    public async Task BulkArchiveDoors_WithUnknownDoorId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-archive",
            new { doors = new object[] { new { doorId = 999999, version = 0u } } });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task BulkArchiveDoors_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لأرشفة جماعية متزامنة");

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/bulk-archive",
            new { doors = new object[] { new { doorId = id, version = 999_999u } } });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
    }

    [Fact]
    public async Task DeleteDoor_ArchivesSubtree_ReturnsNoContent()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentId, parentVersion) = await CreateDoorAsync(client, "أب للحذف");
        var (childId, _) = await CreateDoorAsync(client, "ابن يُؤرشف تلقائيًا", parentId: parentId);

        using var response = await SendWithBodyAsync(client, HttpMethod.Delete, $"/api/abwab/doors/{parentId}", new { version = parentVersion });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();

        var child = await GetDoorAsync(childId);
        child.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteDoor_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لحذف متزامن");

        using var response = await SendWithBodyAsync(client, HttpMethod.Delete, $"/api/abwab/doors/{id}", new { version = 999_999u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
    }

    [Fact]
    public async Task DeleteDoor_WithNullVersion_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لفحص ربط الحذف");
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/abwab/doors/{id}")
        {
            Content = new StringContent("{\"version\": null}", Encoding.UTF8, "application/json"),
        };

        using var response = await client.SendAsync(request);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task DeleteDoor_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await SendWithBodyAsync(client, HttpMethod.Delete, "/api/abwab/doors/999999", new { version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    [Fact]
    public async Task RestoreDoor_RestoresWholeArchivedSubtree()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentId, parentVersion) = await CreateDoorAsync(client, "أب للاستعادة");
        var (childId, _) = await CreateDoorAsync(client, "ابن يُستعاد معه", parentId: parentId);
        using var deleteResponse = await SendWithBodyAsync(client, HttpMethod.Delete, $"/api/abwab/doors/{parentId}", new { version = parentVersion });
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archivedParentVersion = (await GetDoorAsync(parentId)).Version;

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{parentId}/restore", new { version = archivedParentVersion });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var child = await GetDoorAsync(childId);
        child.DeletedAtUtc.Should().BeNull();
    }

    [Fact]
    public async Task RestoreDoor_WhileParentStillArchived_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (parentId, parentVersion) = await CreateDoorAsync(client, "أب يبقى مؤرشفًا");
        var (childId, _) = await CreateDoorAsync(client, "ابن يحاول الاستعادة منفردًا", parentId: parentId);
        using var deleteResponse = await SendWithBodyAsync(client, HttpMethod.Delete, $"/api/abwab/doors/{parentId}", new { version = parentVersion });
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archivedChildVersion = (await GetDoorAsync(childId)).Version;

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{childId}/restore", new { version = archivedChildVersion });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorParentStillArchived);
    }

    [Fact]
    public async Task RestoreDoor_WithStaleVersion_ReturnsConflict()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, version) = await CreateDoorAsync(client, "باب لاستعادة متزامنة");
        using var deleteResponse = await SendWithBodyAsync(client, HttpMethod.Delete, $"/api/abwab/doors/{id}", new { version });
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{id}/restore", new { version = 999_999u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.Conflict, ApiMessages.AbwabDoorStaleVersion);
    }

    [Fact]
    public async Task RestoreDoor_AfterArchive_ReturnsDoorToAContiguousScope()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        await CreateDoorAsync(client, "الأول");
        var (secondId, secondVersion) = await CreateDoorAsync(client, "الثاني");
        await CreateDoorAsync(client, "الثالث");

        using var deleteResponse = await SendWithBodyAsync(
            client, HttpMethod.Delete, $"/api/abwab/doors/{secondId}", new { version = secondVersion });
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archivedVersion = (await GetDoorAsync(secondId)).Version;

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{secondId}/restore", new { version = archivedVersion });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderValues = await GetDoorOrderValuesAsync(null, null);
        orderValues.Values.Order().Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    // The detach is a silent mutation unless the response says so: SectionId comes back null either way,
    // and no caller can tell "was never in a section" from "its section was retired meanwhile".
    [Fact]
    public async Task RestoreDoor_WhenSectionWasArchivedMeanwhile_ReportsTheDetachInThePayload()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم يُؤرشف بعد بابه");
        var (doorId, doorVersion) = await CreateDoorAsync(client, "باب يعود بلا قسم", sectionId: sectionId);
        using var archiveResponse = await SendWithBodyAsync(
            client, HttpMethod.Delete, $"/api/abwab/doors/{doorId}", new { version = doorVersion });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var sectionDeleteResponse = await client.DeleteAsync($"/api/abwab/sections/{sectionId}");
        sectionDeleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archivedVersion = (await GetDoorAsync(doorId)).Version;

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{doorId}/restore", new { version = archivedVersion });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("detachedFromArchivedSection").GetBoolean().Should().BeTrue();
        data.GetProperty("door").GetProperty("sectionId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task RestoreDoor_IntoALiveSection_ReportsNoDetachAndKeepsTheSection()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم يبقى حيًّا بعد الاستعادة");
        var (doorId, doorVersion) = await CreateDoorAsync(client, "باب يعود إلى قسمه", sectionId: sectionId);
        using var archiveResponse = await SendWithBodyAsync(
            client, HttpMethod.Delete, $"/api/abwab/doors/{doorId}", new { version = doorVersion });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var archivedVersion = (await GetDoorAsync(doorId)).Version;

        using var response = await client.PostAsJsonAsync($"/api/abwab/doors/{doorId}/restore", new { version = archivedVersion });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await ApiEnvelope.ReadDataAsync(response);
        data.GetProperty("detachedFromArchivedSection").GetBoolean().Should().BeFalse();
        data.GetProperty("door").GetProperty("sectionId").GetInt32().Should().Be(sectionId);
    }

    [Fact]
    public async Task RestoreDoor_WithNullVersion_ReturnsBadRequestBindingLevel()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (id, _) = await CreateDoorAsync(client, "باب لفحص ربط الاستعادة");
        using var content = new StringContent("{\"version\": null}", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync($"/api/abwab/doors/{id}/restore", content);

        await AssertBindingLevelBadRequestAsync(response);
    }

    [Fact]
    public async Task RestoreDoor_WithUnknownId_ReturnsNotFound()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/abwab/doors/999999/restore", new { version = 0u });

        await ApiEnvelope.AssertFailureEnvelopeAsync(response, HttpStatusCode.NotFound, ApiMessages.AbwabDoorNotFound);
    }

    // ---- Tree read (T304 data-tier smoke) ----

    [Fact]
    public async Task GetAbwabTree_AfterWritesThroughRealEndpoints_ReflectsArchivedFlagAndCounts()
    {
        await fixture.ResetAbwabAsync();
        using var client = fixture.CreateClient();

        var (sectionId, _) = await CreateSectionAsync(client, "قسم شجرة القراءة");
        var (parentId, _) = await CreateDoorAsync(client, "أب لقراءة الشجرة", sectionId: sectionId);
        var (childId, childVersion) = await CreateDoorAsync(client, "ابن يُؤرشف لقراءة الشجرة", sectionId: sectionId, parentId: parentId);

        using var archiveResponse = await SendWithBodyAsync(client, HttpMethod.Delete, $"/api/abwab/doors/{childId}", new { version = childVersion });
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var treeResponse = await client.GetAsync("/api/abwab/tree");

        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(treeResponse);
        var data = envelope.GetProperty("data");

        var sectionEntry = data.GetProperty("sections").EnumerateArray()
            .Single(s => s.GetProperty("id").GetInt32() == sectionId);
        sectionEntry.GetProperty("doorsInScopeCount").GetInt32()
            .Should().Be(1, "the archived child no longer counts toward the section's live total");

        var doors = data.GetProperty("doors").EnumerateArray().ToList();
        doors.Single(d => d.GetProperty("id").GetInt32() == childId)
            .GetProperty("isArchived").GetBoolean().Should().BeTrue("archived doors are included and flagged, never omitted");
        doors.Single(d => d.GetProperty("id").GetInt32() == parentId)
            .GetProperty("directChildCount").GetInt32().Should().Be(0, "its only child is archived, so it does not count live");
    }

    // Not AssertFailureEnvelopeAsync: that helper asserts an EMPTY errors array, which fits a handler
    // outcome but not this one — [ApiController]'s own model-state 400 fires before any handler runs and
    // populates errors with the per-field binding message. That distinction is the whole point of these
    // cases: a body that fails at the binding layer must still come back in the shared failure envelope,
    // not as a framework ProblemDetails and not as a 500.
    private static async Task AssertBindingLevelBadRequestAsync(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var envelope = await ApiEnvelope.AssertEnvelopeMatchesStatusAsync(response);
        envelope.GetProperty("isSuccess").GetBoolean().Should().BeFalse();
        envelope.GetProperty("message").GetString().Should().Be(ApiMessages.ValidationFailed);
        envelope.GetProperty("errors").GetArrayLength().Should().BeGreaterThan(0);
    }

    private static async Task<(int Id, uint Version)> CreateSectionAsync(HttpClient client, string name)
    {
        using var response = await client.PostAsJsonAsync("/api/abwab/sections", new { name });
        var data = await ApiEnvelope.ReadDataAsync(response);
        return (data.GetProperty("id").GetInt32(), data.GetProperty("version").GetUInt32());
    }

    private static async Task<(int Id, uint Version)> CreateDoorAsync(
        HttpClient client, string name, int? sectionId = null, int? parentId = null, string[]? aliases = null)
    {
        using var response = await client.PostAsJsonAsync("/api/abwab/doors",
            new { sectionId, parentId, name, aliases = aliases ?? [] });
        var data = await ApiEnvelope.ReadDataAsync(response);
        return (data.GetProperty("id").GetInt32(), data.GetProperty("version").GetUInt32());
    }

    private static Task<HttpResponseMessage> SendWithBodyAsync(HttpClient client, HttpMethod method, string path, object body)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body) };
        return client.SendAsync(request);
    }

    private async Task<(int? SectionId, int? ParentId, int OrderValue, uint Version, DateTimeOffset? DeletedAtUtc)> GetDoorAsync(int id)
    {
        using var scope = fixture.ApiServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        var door = await db.AbwabDoors.AsNoTracking().SingleAsync(d => d.Id == id);
        return (door.SectionId, door.ParentId, door.OrderValue, door.Version, door.DeletedAtUtc);
    }

    private async Task<IReadOnlyDictionary<int, int>> GetDoorOrderValuesAsync(int? sectionId, int? parentId)
    {
        using var scope = fixture.ApiServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuranDashboardDbContext>();
        return await db.AbwabDoors.AsNoTracking()
            .Where(d => d.SectionId == sectionId && d.ParentId == parentId && d.DeletedAtUtc == null)
            .ToDictionaryAsync(d => d.Id, d => d.OrderValue);
    }
}
