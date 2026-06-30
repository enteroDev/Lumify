namespace lumify.api.Models.DTO.Requests;

/// <summary>
/// Request body for updating a todo entry
/// (see <see cref="Controllers.TodoListsController.SaveTodoEntry"/>). Only non-<c>null</c> fields
/// are applied (partial update); changing the status may flip the parent list's status.
/// </summary>
public class SaveTodoEntryRequest
{
    /// <summary>The entry to update (required).</summary>
    public string ID { get; set; } = "";
    /// <summary>New title, if changing.</summary>
    public string? Name { get; set; }
    /// <summary>New description, if changing.</summary>
    public string? Description { get; set; }
    /// <summary>New status (1 = pending, 2 = done), if changing.</summary>
    public int? Status { get; set; }
}