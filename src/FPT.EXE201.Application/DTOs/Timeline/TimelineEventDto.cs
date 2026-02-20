namespace FPT.EXE201.Application.DTOs.Timeline;

/// <summary>
/// Một sự kiện trên dòng thời gian thai kỳ.
/// EventType: "Document", "Visit".
/// </summary>
public record TimelineEventDto(
    /// <summary>Loại sự kiện: "Document", "Visit".</summary>
    string EventType,

    /// <summary>ID của entity (document ID hoặc visit ID).</summary>
    Guid EventId,

    /// <summary>Ngày xảy ra sự kiện.</summary>
    DateTime EventDate,

    /// <summary>Tiêu đề hiển thị.</summary>
    string? Title,

    /// <summary>Mô tả ngắn.</summary>
    string? Description
);
