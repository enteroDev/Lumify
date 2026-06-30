

namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A todo list returned to clients (see <see cref="Controllers.TodoListsController"/>).
    /// </summary>
    public class TodoListResponse
    {
        /// <summary>List ID.</summary>
        public string ID { get; set; } = "";
        /// <summary>The owner's user ID.</summary>
        public string OwnerID { get; set; } = "";
        /// <summary>The owning workspace, or <c>null</c> for a personal list.</summary>
        public string? WorkspaceID { get; set; }
        /// <summary>List name.</summary>
        public string Name { get; set; } = "";
        /// <summary>Status: 1 = open, 2 = done.</summary>
        public int Status { get; set; }
        /// <summary>Archived flag (0 = active, 1 = archived).</summary>
        public int IsArchived { get; set; }
        /// <summary>Creation timestamp (ISO-8601 string).</summary>
        public string CreatedAt { get; set; } = "";
        /// <summary>Last-update timestamp (ISO-8601 string).</summary>
        public string UpdatedAt { get; set; } = "";
    }
}