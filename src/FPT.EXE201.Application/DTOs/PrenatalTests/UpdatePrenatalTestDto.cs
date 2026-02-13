namespace FPT.EXE201.Application.DTOs.PrenatalTests;

public record UpdatePrenatalTestDto(
    /// <summary>
    /// URLs ảnh cũ muốn giữ lại. Ảnh cũ không nằm trong list này sẽ bị xóa khỏi storage.
    /// </summary>
    List<string>? ExistingImageUrls,

    string? Notes,
    bool IsAbnormalResult
);
