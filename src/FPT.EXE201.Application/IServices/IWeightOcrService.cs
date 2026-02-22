using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Weight OCR extraction — interface ở Application layer.
/// Implementation ở Infrastructure layer (dùng IOcrProvider).
/// </summary>
public interface IWeightOcrService
{
    Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(
        Stream imageStream, string fileName, CancellationToken ct = default);
}
