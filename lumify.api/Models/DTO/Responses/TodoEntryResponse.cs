

namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A todo entry returned to clients (see <see cref="Controllers.TodoListsController"/>).
    /// </summary>
    public class TodoEntryResponse
    {
        /// <summary>Entry ID.</summary>
        public string ID { get; set; } = "";
        /// <summary>The parent todo list.</summary>
        public string TodoListID { get; set; } = "";
        /// <summary>The owner's user ID.</summary>
        public string OwnerID { get; set; } = "";

        /// <summary>Entry title.</summary>
        public string Name { get; set; } = "";
        /// <summary>Optional entry description.</summary>
        public string? Description { get; set; }
        /// <summary>Status: 1 = open, 2 = done.</summary>
        public int Status { get; set; }

        /// <summary>True if this update was the last open entry being checked (so the list became done).</summary>
        public bool WasLastUnchecked { get; set; } = false;

        /// <summary>Creation timestamp (ISO-8601 string).</summary>
        public string CreatedAt { get; set; } = "";
        /// <summary>Last-update timestamp (ISO-8601 string).</summary>
        public string UpdatedAt { get; set; } = "";
    }
}