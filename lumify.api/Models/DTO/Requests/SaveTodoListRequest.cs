namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for updating a todo list
/// (see <see cref="Controllers.TodoListsController.SaveTodoList"/>). Only non-<c>null</c> fields
/// are applied (partial update).
/// </summary>
public sealed class SaveTodoListRequest
{
    /// <summary>The list to update (required).</summary>
    public string ID { get; set; } = "";
    /// <summary>New name, if changing.</summary>
    public string? Name { get; set; }
    /// <summary>New status (1 = pending, 2 = done), if changing.</summary>
    public int? Status { get; set; }
    /// <summary>New archived state, if changing.</summary>
    public bool? IsArchived { get; set; }
}