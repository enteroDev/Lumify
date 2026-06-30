namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A workspace returned to clients (see <see cref="Controllers.WorkspaceController"/>).
    /// </summary>
    public class WorkspaceResponse
    {
        /// <summary>Workspace ID.</summary>
        public string ID { get; set; } = string.Empty;
        /// <summary>The owner's user ID.</summary>
        public string OwnerID { get; set; } = string.Empty;
        /// <summary>Workspace name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Creation timestamp (ISO-8601 string).</summary>
        public string CreatedAt { get; set; } = string.Empty;
        /// <summary>Last-update timestamp (ISO-8601 string).</summary>
        public string UpdatedAt { get; set; } = string.Empty;
    }
}