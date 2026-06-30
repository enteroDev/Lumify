namespace lumify.api.Models.DTO.Requests
{
    /// <summary>
    /// Request body for adding an entry to a todo list
    /// (see <see cref="Controllers.TodoListsController.AddTodoEntry"/>).
    /// </summary>
    public class AddTodoEntryRequest
    {
        /// <summary>The parent todo list (required).</summary>
        public string? TodoListID { get; set; }
        /// <summary>Entry title (required).</summary>
        public string Name { get; set; } = "";
        /// <summary>Optional entry description.</summary>
        public string? Description { get; set; }
    }
}