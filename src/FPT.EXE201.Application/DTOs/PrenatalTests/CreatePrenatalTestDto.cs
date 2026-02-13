namespace FPT.EXE201.Application.DTOs.PrenatalTests;

public record CreatePrenatalTestDto(
    /// <summary>ID loại xét nghiệm từ danh mục (ref_test_types).</summary>
    Guid TestTypeId,

    /// <summary>Buổi khám liên kết (optional). Phải thuộc cùng pregnancy.</summary>
    Guid? VisitId,

    DateOnly TestDate,

    /// <summary>Ghi chú tự do (optional).</summary>
    string? Notes,

    bool IsAbnormalResult = false
);
