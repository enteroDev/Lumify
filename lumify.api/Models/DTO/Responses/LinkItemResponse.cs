namespace lumify.api.Models.DTO.Responses
{
    /// <summary>
    /// A link item module returned to clients (see <see cref="Controllers.NotesController"/>).
    /// </summary>
    public class LinkItemResponse
    {
        /// <summary>Link item ID.</summary>
        public string ID { get; set; } = null!;
        /// <summary>The parent note.</summary>
        public string NoteID { get; set; } = null!;
        /// <summary>Optional display label for the link.</summary>
        public string? Label { get; set; }
        /// <summary>The target URL.</summary>
        public string Url { get; set; } = null!;
        /// <summary>Position within the note.</summary>
        public int NotePos { get; set; }
        /// <summary>Creation timestamp (ISO-8601 string).</summary>
        public string CreatedAt { get; set; } = null!;
        /// <summary>Last-update timestamp (ISO-8601 string).</summary>
        public string UpdatedAt { get; set; } = null!;
    }
}