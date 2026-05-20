namespace lumify.api.Models.DTO.Requests
{
    public class AddTodoListRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? WorkspaceID { get; set; }
    }
}