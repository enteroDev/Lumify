namespace lumify.api.Models.DTO.Requests
{
    /// <summary>
    /// Request body for creating a todo list
    /// (see <see cref="Controllers.TodoListsController.AddTodoList"/>).
    /// </summary>
    public class AddTodoListRequest
    {
        /// <summary>List name (required).</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Target workspace, or <c>null</c> for a personal list.</summary>
        public string? WorkspaceID { get; set; }
    }
}