namespace lumify.api.Models.DTO.Responses
{
    public class LinkItemResponse
    {
        public string ID { get; set; } = null!;
        public string NoteID { get; set; } = null!;
        public string? Label { get; set; }
        public string Url { get; set; } = null!;
        public int NotePos { get; set; }
        public string CreatedAt { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}