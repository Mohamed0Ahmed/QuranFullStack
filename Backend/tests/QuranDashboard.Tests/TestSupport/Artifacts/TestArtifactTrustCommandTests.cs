using QuranDashboard.TestArtifacts;
using System.Text.Json.Nodes;

namespace QuranDashboard.Tests.TestSupport.Artifacts;

public sealed class TestArtifactTrustCommandTests
{
    [Fact]
    public void Verify_TableFamilyFlagsMustMatchDeclaredTables()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.SetTableFamily("quran", present: false);
        repository.SetTableFamily("access", present: true);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll("artifact=lock", "family flags");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_PhraseSearchTableScopeRequiresTrustMetadata()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.AddPhraseSearchTableWithoutTrustMetadata();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll("artifact=lock", "PhraseSearch trust metadata");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Status_EfDesignerAndSnapshotFilesDoNotInflateMigrationState()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["status", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("state=present");
        output.ToString().Should().NotContain("Designer");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_ManifestOnlyLockEntryFailsClosed()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.RemovePayloadEntryFromLock();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll("artifact=lock", "payload file");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_MutableStorageAliasFailsClosed()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact(
            immutableStorageId: "https://storage.example/artifacts/latest");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll("artifact=lock", "immutable logical identifier");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_VersionMustBeginWithAnAlphanumericCharacter()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.SetArtifactVersion(".2026.08.31");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll("artifact=lock", "version is invalid");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_PhraseSearchBuildIdLivesOnlyInHashedExternalManifest()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.EnablePhraseSearchWithExternalBuildId();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("state=present");
        repository.LockText.Should().NotContain("activeBuildId");
        repository.ManifestText.Should().Contain("activeBuildId");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_SameSizePayloadHashDriftReportsMismatched()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.TamperPayloadPreservingSize();
        using var statusOutput = new StringWriter();
        using var verifyOutput = new StringWriter();
        using var error = new StringWriter();

        var statusExitCode = ArtifactTrustCommand.Execute(
            ["status", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            statusOutput,
            error);
        var verifyExitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            verifyOutput,
            error);

        statusExitCode.Should().Be(0);
        statusOutput.ToString().Should().Contain("state=present");
        verifyExitCode.Should().Be(1);
        verifyOutput.ToString().Should().ContainAll("state=mismatched", "sha256 mismatch");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_UnexpectedManifestMemberFailsStrictShapeValidation()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.AddUnexpectedManifestMemberAndRelock();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll(
            "state=mismatched",
            "credential",
            "could not be mapped");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_VolatilePhraseSearchBuildIdInLockFailsStrictShapeValidation()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.AddVolatilePhraseSearchBuildIdToLock();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll(
            "artifact=lock",
            "state=mismatched",
            "activeBuildId");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Status_RequiredLaneSelectsItsLockedArtifact()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["status", "--lane", "critical", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(0);
        output.ToString().Should().ContainAll(
            "artifact=compact-cross-stack-base",
            "state=present",
            "summary required=1 present=1 missing=0 stale=0 mismatched=0");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_CredentialBearingStorageIdentityFailsClosed()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact(
            immutableStorageId: "https://artifact-user:artifact-secret@example.test/fixture?sig=volatile");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll(
            "artifact=lock",
            "state=mismatched",
            "credential-free");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Status_LockedMigrationBehindTreeReportsStale()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact(
            migrationHead: "20260817163513_AddAbwabDoorInclusionSynchronization",
            migrationCount: 5);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["status", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll(
            "state=stale",
            "20260826012918_AddQuranPhraseSearchIndex",
            "count 6");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_UnsafeManifestTableIdentifierFailsBeforeUse()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.SetManifestTableAndRelock("quran_ayahs;drop_table");
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll(
            "state=mismatched",
            "invalid table identifier");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Status_MissingArtifactReportsMissing()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        repository.RemovePayload();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["status", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        output.ToString().Should().ContainAll(
            "artifact=compact-cross-stack-base",
            "required=true",
            "state=missing");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_TrustedArtifactReportsPresent()
    {
        using var repository = TemporaryRepository.CreateWithTrustedArtifact();
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(0);
        output.ToString().Should().ContainAll(
            "artifact=compact-cross-stack-base",
            "required=true",
            "state=present");
        error.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Verify_UnknownArtifactFailsClosed()
    {
        using var repository = TemporaryRepository.CreateWithLock(
            """
            {
              "$schema": "docs/testing/test-artifacts-lock.schema.json",
              "contractVersion": 1,
              "artifacts": []
            }
            """);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = ArtifactTrustCommand.Execute(
            ["verify", "--artifact", "compact-cross-stack-base", "--root", repository.Root],
            output,
            error);

        exitCode.Should().Be(1);
        error.ToString().Should().Contain("not locked");
    }

    private sealed class TemporaryRepository : IDisposable
    {
        private const string ArtifactDirectory = "artifacts/compact-cross-stack-base";
        private const string PayloadRelativePath = $"{ArtifactDirectory}/fixture.dump";
        private const string ManifestRelativePath = $"{ArtifactDirectory}/manifest.json";

        private TemporaryRepository(string root)
        {
            Root = root;
        }

        internal string Root { get; }

        internal string LockText =>
            File.ReadAllText(Path.Combine(Root, "test-artifacts.lock.json"));

        internal string ManifestText =>
            File.ReadAllText(Path.Combine(Root, ManifestRelativePath));

        internal void RemovePayload()
        {
            File.Delete(Path.Combine(Root, PayloadRelativePath));
        }

        internal void TamperPayloadPreservingSize()
        {
            var path = Path.Combine(Root, PayloadRelativePath);
            var bytes = File.ReadAllBytes(path);
            bytes[0] ^= 1;
            File.WriteAllBytes(path, bytes);
        }

        internal void SetManifestTableAndRelock(string tableName)
        {
            var manifest = ReadObject(ManifestRelativePath);
            manifest["tables"]!.AsArray()[0]!["name"] = tableName;
            WriteManifestAndRelock(manifest);
        }

        internal void AddUnexpectedManifestMemberAndRelock()
        {
            var manifest = ReadObject(ManifestRelativePath);
            manifest["credential"] = "must-not-be-accepted";
            WriteManifestAndRelock(manifest);
        }

        internal void AddVolatilePhraseSearchBuildIdToLock()
        {
            var artifactLock = ReadObject("test-artifacts.lock.json");
            var artifact = artifactLock["artifacts"]!.AsArray()[0]!.AsObject();
            artifact["phraseSearch"] = new JsonObject
            {
                ["manifestSha256"] = new string('e', 64),
                ["sourceFingerprint"] = "fixture-source-fingerprint",
                ["readinessExpectation"] = "available",
                ["activeBuildId"] = "volatile-build-id",
            };
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        internal void RemovePayloadEntryFromLock()
        {
            var artifactLock = ReadObject("test-artifacts.lock.json");
            var stagedFiles = artifactLock["artifacts"]!.AsArray()[0]!["stagedFiles"]!.AsArray();
            var payload = stagedFiles
                .Single(file => file!["role"]!.GetValue<string>() == "payload");
            stagedFiles.Remove(payload);
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        internal void SetArtifactVersion(string version)
        {
            var artifactLock = ReadObject("test-artifacts.lock.json");
            artifactLock["artifacts"]!.AsArray()[0]!["version"] = version;
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        internal void SetTableFamily(string family, bool present)
        {
            var artifactLock = ReadObject("test-artifacts.lock.json");
            artifactLock["artifacts"]!.AsArray()[0]!["tableScope"]![family] = present;
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        internal void EnablePhraseSearchWithExternalBuildId()
        {
            AddPhraseSearchTableWithoutTrustMetadata();
            var manifest = ReadObject(ManifestRelativePath);
            manifest["phraseSearch"] = new JsonObject
            {
                ["sourceFingerprint"] = "fixture-source-fingerprint",
                ["readiness"] = "available",
                ["activeBuildId"] = "external-build-id",
            };
            WriteManifestAndRelock(manifest);

            var artifactLock = ReadObject("test-artifacts.lock.json");
            var artifact = artifactLock["artifacts"]!.AsArray()[0]!.AsObject();
            artifact["phraseSearch"] = new JsonObject
            {
                ["manifestSha256"] = Sha256(Path.Combine(Root, ManifestRelativePath)),
                ["sourceFingerprint"] = "fixture-source-fingerprint",
                ["readinessExpectation"] = "available",
            };
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        internal void AddPhraseSearchTableWithoutTrustMetadata()
        {
            var manifest = ReadObject(ManifestRelativePath);
            manifest["tables"]!.AsArray().Add(new JsonObject
            {
                ["name"] = "quran_phrase_index_state",
                ["rows"] = 1,
            });
            WriteManifestAndRelock(manifest);

            var artifactLock = ReadObject("test-artifacts.lock.json");
            var tableScope = artifactLock["artifacts"]!.AsArray()[0]!["tableScope"]!;
            tableScope["phraseSearch"] = true;
            tableScope["tables"]!.AsArray().Add("quran_phrase_index_state");
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        internal static TemporaryRepository CreateWithLock(string json)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"quran-dashboard-test-artifacts-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "test-artifacts.lock.json"), json);
            return new TemporaryRepository(root);
        }

        internal static TemporaryRepository CreateWithTrustedArtifact(
            string tableName = "quran_ayahs",
            string migrationHead = "20260826012918_AddQuranPhraseSearchIndex",
            int migrationCount = 6,
            string? immutableStorageId = null)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                $"quran-dashboard-test-artifacts-{Guid.NewGuid():N}");
            var stagedDirectory = Path.Combine(root, ArtifactDirectory);
            Directory.CreateDirectory(stagedDirectory);
            CreateMigrationTree(root);

            var payload = "artifact contract vector\n";
            File.WriteAllText(Path.Combine(root, PayloadRelativePath), payload);

            var manifest = JsonSerializer.Serialize(new
            {
                contractVersion = 1,
                artifactId = "compact-cross-stack-base",
                artifactVersion = "2026.08.31.1",
                migration = new
                {
                    head = migrationHead,
                    count = migrationCount,
                },
                postgresql = new
                {
                    producerVersion = "18.0",
                },
                producer = new
                {
                    command = "contract-vector-producer --compact-cross-stack-base",
                    version = "1.0.0",
                },
                tables = new[]
                {
                    new { name = tableName, rows = 1 },
                },
                sources = new[]
                {
                    new
                    {
                        id = "contract-vector-source",
                        version = "2026.08.31",
                        sha256 = new string('a', 64),
                        provenance = "Synthetic contract vector; not an artifact source.",
                    },
                },
                sentinels = new[]
                {
                    new
                    {
                        id = "ayah-1-1",
                        expectedCount = 1,
                        oracleSha256 = new string('b', 64),
                    },
                },
            });
            File.WriteAllText(Path.Combine(root, ManifestRelativePath), manifest);

            var artifactLock = JsonSerializer.Serialize(new
            {
                schema = "docs/testing/test-artifacts-lock.schema.json",
                contractVersion = 1,
                artifacts = new[]
                {
                    new
                    {
                        id = "compact-cross-stack-base",
                        version = "2026.08.31.1",
                        requiredLanes = new[] { "critical" },
                        stagedFiles = new object[]
                        {
                            new
                            {
                                path = PayloadRelativePath,
                                role = "payload",
                                size = new FileInfo(Path.Combine(root, PayloadRelativePath)).Length,
                                sha256 = Sha256(Path.Combine(root, PayloadRelativePath)),
                            },
                            new
                            {
                                path = ManifestRelativePath,
                                role = "manifest",
                                size = new FileInfo(Path.Combine(root, ManifestRelativePath)).Length,
                                sha256 = Sha256(Path.Combine(root, ManifestRelativePath)),
                            },
                        },
                        manifestPath = ManifestRelativePath,
                        migration = new
                        {
                            head = migrationHead,
                            count = migrationCount,
                        },
                        tableScope = new
                        {
                            quran = true,
                            phraseSearch = false,
                            abwab = false,
                            access = false,
                            linking = false,
                            tables = new[] { tableName },
                        },
                        postgresql = new
                        {
                            producerVersion = "18.0",
                            containerDigest = $"sha256:{new string('c', 64)}",
                        },
                        producer = new
                        {
                            command = "contract-vector-producer --compact-cross-stack-base",
                            version = "1.0.0",
                        },
                        sources = new[]
                        {
                            new
                            {
                                id = "contract-vector-source",
                                version = "2026.08.31",
                                sha256 = new string('a', 64),
                                provenance = "Synthetic contract vector; not an artifact source.",
                            },
                        },
                        sentinels = new[]
                        {
                            new
                            {
                                id = "ayah-1-1",
                                expectedCount = 1,
                                oracleSha256 = new string('b', 64),
                            },
                        },
                        immutableStorageId = immutableStorageId
                            ?? $"qdb-artifact:compact-cross-stack-base@sha256:{new string('d', 64)}",
                        refresh = new
                        {
                            date = "2026-08-31",
                            reason = "Initial compact fixture contract test.",
                            ownerRole = "test-infrastructure-maintainer",
                        },
                    },
                },
            });

            artifactLock = artifactLock.Replace(
                "\"schema\":",
                "\"$schema\":",
                StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "test-artifacts.lock.json"), artifactLock);
            return new TemporaryRepository(root);
        }

        private JsonObject ReadObject(string relativePath)
        {
            return JsonNode.Parse(File.ReadAllText(Path.Combine(Root, relativePath)))!.AsObject();
        }

        private void WriteManifestAndRelock(JsonObject manifest)
        {
            var manifestPath = Path.Combine(Root, ManifestRelativePath);
            File.WriteAllText(manifestPath, manifest.ToJsonString());

            var artifactLock = ReadObject("test-artifacts.lock.json");
            var stagedFiles = artifactLock["artifacts"]!.AsArray()[0]!["stagedFiles"]!.AsArray();
            var manifestFile = stagedFiles
                .Select(file => file!.AsObject())
                .Single(file => file["role"]!.GetValue<string>() == "manifest");
            manifestFile["size"] = new FileInfo(manifestPath).Length;
            manifestFile["sha256"] = Sha256(manifestPath);
            File.WriteAllText(
                Path.Combine(Root, "test-artifacts.lock.json"),
                artifactLock.ToJsonString());
        }

        private static void CreateMigrationTree(string root)
        {
            var migrationsDirectory = Path.Combine(
                root,
                "Backend/infrastructure/QuranDashboard.Infrastructure/Migrations");
            Directory.CreateDirectory(migrationsDirectory);
            foreach (var migration in new[]
                     {
                         "20260813153400_InitialBaseline.cs",
                         "20260814153559_M2DurablePreparedLinkingPreflight.cs",
                         "20260814212547_M3DurableLinkingConfirmationJobs.cs",
                         "20260815175846_AddUserDeviceSessions.cs",
                         "20260817163513_AddAbwabDoorInclusionSynchronization.cs",
                         "20260826012918_AddQuranPhraseSearchIndex.cs",
                     })
            {
                File.WriteAllText(Path.Combine(migrationsDirectory, migration), string.Empty);
                File.WriteAllText(
                    Path.Combine(
                        migrationsDirectory,
                        migration.Replace(".cs", ".Designer.cs", StringComparison.Ordinal)),
                    string.Empty);
            }

            File.WriteAllText(
                Path.Combine(migrationsDirectory, "QuranDashboardDbContextModelSnapshot.cs"),
                string.Empty);
        }

        private static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }

        public void Dispose()
        {
            Directory.Delete(Root, recursive: true);
        }
    }
}
