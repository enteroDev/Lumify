namespace lumify.api.Models.Enum
{
    /// <summary>
    /// A user's real-time presence state, derived from their active connections by the
    /// <see cref="Services.PresenceService"/>.
    /// </summary>
    public enum PresenceStatus
    {
        /// <summary>No active connection.</summary>
        Offline = 0,
        /// <summary>At least one active connection.</summary>
        Online = 1,
        /// <summary>Connected but marked away.</summary>
        Away = 2,
        /// <summary>Connected but not to be disturbed.</summary>
        DoNotDisturb = 3
    }
}