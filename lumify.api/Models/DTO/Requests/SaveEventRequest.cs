

namespace lumify.api.Models.DTO.Requests
{
    /// <summary>
    /// Request body for updating an event (see <see cref="Controllers.EventsController.SaveEvent"/>).
    /// Only non-<c>null</c> fields are applied (partial update).
    /// </summary>
    public class SaveEventRequest
    {
        /// <summary>The event to update (required).</summary>
        public string ID { get; set; } = string.Empty;
        /// <summary>New title, if changing.</summary>
        public string? Name { get; set; }
        /// <summary>New description, if changing.</summary>
        public string? Description { get; set; }
        /// <summary>New all-day flag, if changing.</summary>
        public bool? IsAllDay { get; set; }
        /// <summary>New start date/time, if changing.</summary>
        public string? StartTime { get; set; }
        /// <summary>New end date/time, if changing.</summary>
        public string? EndTime { get; set; }
    }
}