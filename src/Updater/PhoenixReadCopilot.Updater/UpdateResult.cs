using Semver;

namespace PhoenixReadCopilot.Updater;

public sealed record UpdateResult(
    UpdateOutcome Outcome,
    SemVersion? Version = null,
    string? Message = null
);
