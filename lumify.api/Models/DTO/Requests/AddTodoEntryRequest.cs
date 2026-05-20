namespace lumify.api.Models.DTO.Requests
{
    public class AddTodoEntryRequest
    {
        public string? TodoListID { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }
}