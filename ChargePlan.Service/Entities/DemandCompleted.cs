public record DemandCompleted(
    string UniqueId, // Allows the algorithm to determine that a specific demand has been run.
    DateTime DateTime, // Helps with pruning old values away.
    string Name // Human readable name for convenience.
);