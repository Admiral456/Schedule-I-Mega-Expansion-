using ScheduleIMegaExpansion.Core.Network;
using ScheduleIMegaExpansion.Core.Persistence;
using Xunit;

namespace ScheduleIMegaExpansion.Tests;

public sealed class NetworkAndPersistenceTests
{
    [Fact]
    public void ValidatorRejectsSpoofedSender()
    {
        var message = new NetworkMessageEnvelope(1, "session-1", "steam-attacker", 0, "ShopPurchaseRequested", "req-1", "{}");

        NetworkValidationResult result = NetworkMessageValidator.Validate(
            message, "steam-authenticated", "session-1", 1);

        Assert.False(result.IsValid);
        Assert.Equal(NetworkValidationError.SenderMismatch, result.Error);
    }

    [Fact]
    public void ValidatorRejectsMutationWithoutRequestId()
    {
        var message = new NetworkMessageEnvelope(1, "session-1", "steam-player", 1, "ShopPurchaseRequested", null, "{}");

        NetworkValidationResult result = NetworkMessageValidator.Validate(
            message, "steam-player", "session-1", 1);

        Assert.False(result.IsValid);
        Assert.Equal(NetworkValidationError.MissingRequestId, result.Error);
    }

    [Fact]
    public void ValidatorRejectsWrongSessionAndOversizedPayload()
    {
        var wrongSession = new NetworkMessageEnvelope(1, "other", "steam-player", 0, "SnapshotRequest", null, "{}");
        Assert.Equal(
            NetworkValidationError.SessionMismatch,
            NetworkMessageValidator.Validate(wrongSession, "steam-player", "session-1", 1).Error);

        var oversized = new NetworkMessageEnvelope(1, "session-1", "steam-player", 0, "SnapshotRequest", null, "{\"value\":\"12345\"}");
        Assert.Equal(
            NetworkValidationError.PayloadTooLarge,
            NetworkMessageValidator.Validate(oversized, "steam-player", "session-1", 1, maximumPayloadBytes: 8).Error);
    }

    [Fact]
    public void ValidatorRejectsMalformedPayloadAndUnknownMessageType()
    {
        var malformed = new NetworkMessageEnvelope(1, "session-1", "steam-player", 0, "SnapshotRequest", null, "{oops}");
        Assert.Equal(
            NetworkValidationError.InvalidPayloadJson,
            NetworkMessageValidator.Validate(malformed, "steam-player", "session-1", 1).Error);

        var unknown = new NetworkMessageEnvelope(1, "session-1", "steam-player", 0, "ArbitraryCommand", null, "{}");
        Assert.Equal(
            NetworkValidationError.UnknownMessageType,
            NetworkMessageValidator.Validate(unknown, "steam-player", "session-1", 1).Error);
    }

    [Fact]
    public void ReplayWindowAllowsLimitedReorderingButRejectsReplayAndStaleSequence()
    {
        var window = new ReplayWindow();

        Assert.True(window.TryAccept(10));
        Assert.True(window.TryAccept(12));
        Assert.True(window.TryAccept(11));
        Assert.False(window.TryAccept(11));
        Assert.False(window.TryAccept(10));
        Assert.Equal(12, window.HighestSequence);

        Assert.True(window.TryAccept(100));
        Assert.False(window.TryAccept(36)); // 64 behind the current high-water mark.
        Assert.False(window.TryAccept(-1));
    }

    [Fact]
    public void MigrationPipelineRunsEveryVersionInOrder()
    {
        var pipeline = new SaveMigrationPipeline<Dictionary<string, int>>(
            "schedulei.megaexpansion",
            3,
            new[]
            {
                new SaveMigrationStep<Dictionary<string, int>>(1, 2, data =>
                {
                    var copy = new Dictionary<string, int>(data);
                    copy["schema2"] = 2;
                    return copy;
                }),
                new SaveMigrationStep<Dictionary<string, int>>(2, 3, data =>
                {
                    var copy = new Dictionary<string, int>(data);
                    copy["schema3"] = 3;
                    return copy;
                })
            });

        var sourceData = new Dictionary<string, int>();
        var migrated = pipeline.Migrate(new ModSaveDocument<Dictionary<string, int>>(
            "schedulei.megaexpansion", 1, "world-1", 7, sourceData));

        Assert.Equal(3, migrated.SchemaVersion);
        Assert.Equal(2, migrated.Data["schema2"]);
        Assert.Equal(3, migrated.Data["schema3"]);
        Assert.Equal(7, migrated.WorldRevision);
        Assert.Empty(sourceData); // The migration did not mutate the original test fixture.
    }

    [Fact]
    public void MigrationPipelineFailsClosedForMissingStepsOrWrongNamespace()
    {
        var missingStepPipeline = new SaveMigrationPipeline<Dictionary<string, int>>(
            "schedulei.megaexpansion",
            3,
            new[] { new SaveMigrationStep<Dictionary<string, int>>(1, 2, data => new Dictionary<string, int>(data)) });

        Assert.Throws<InvalidOperationException>(() =>
            missingStepPipeline.Migrate(new ModSaveDocument<Dictionary<string, int>>(
                "schedulei.megaexpansion", 1, "world-1", 0, new Dictionary<string, int>())));

        var currentPipeline = new SaveMigrationPipeline<Dictionary<string, int>>(
            "schedulei.megaexpansion", 1, Array.Empty<SaveMigrationStep<Dictionary<string, int>>>());

        Assert.Throws<InvalidOperationException>(() =>
            currentPipeline.Migrate(new ModSaveDocument<Dictionary<string, int>>(
                "different.mod", 1, "world-1", 0, new Dictionary<string, int>())));
    }

    [Fact]
    public void JsonSaveStoreKeepsPreviousValidFileAsBackup()
    {
        string directory = Path.Combine(Path.GetTempPath(), "sime-save-tests-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "expansion.save.json");

        try
        {
            var store = new JsonSaveStore<TestSaveDocument>(path);
            store.Write(new TestSaveDocument(1, "first"));
            store.Write(new TestSaveDocument(2, "second"));

            Assert.Equal(new TestSaveDocument(2, "second"), store.Read());
            Assert.Equal(new TestSaveDocument(1, "first"), store.ReadBackup());
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    public sealed record TestSaveDocument(int Revision, string Value);
}
