using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using System.Text.RegularExpressions;

namespace FPT.EXE201.Infrastructure.Services;

/// <summary>
/// OCR weight extraction — Infrastructure implementation.
/// Wraps IOcrProvider (AzureOcrProvider) + regex parsing logic.
/// </summary>
public class WeightOcrService : IWeightOcrService
{
    private readonly IOcrProvider _ocrProvider;

    public WeightOcrService(IOcrProvider ocrProvider)
    {
        _ocrProvider = ocrProvider;
    }

    public async Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(
        Stream imageStream, string fileName, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new BadRequestException("Only JPEG and PNG images are supported.")
        };

        try
        {
            var ocrRequest = new OcrRequest(imageStream, fileName, contentType, "vi");
            var ocrResponse = await _ocrProvider.ExtractTextAsync(ocrRequest, ct);

            if (string.IsNullOrWhiteSpace(ocrResponse.RawText))
            {
                return new WeightOcrExtractResultDto(
                    Success: false,
                    ExtractedWeightKg: null,
                    RawOcrText: null,
                    ConfidenceScore: ocrResponse.ConfidenceScore,
                    Message: "Không nhận diện được text từ ảnh. Vui lòng chụp rõ hơn."
                );
            }

            var extractedWeight = ParseWeightFromText(ocrResponse.RawText);

            if (!extractedWeight.HasValue)
            {
                return new WeightOcrExtractResultDto(
                    Success: false,
                    ExtractedWeightKg: null,
                    RawOcrText: ocrResponse.RawText,
                    ConfidenceScore: ocrResponse.ConfidenceScore,
                    Message: "Nhận diện được text nhưng không tìm thấy giá trị cân nặng hợp lệ (30–200 kg). Vui lòng chụp lại."
                );
            }

            return new WeightOcrExtractResultDto(
                Success: true,
                ExtractedWeightKg: extractedWeight.Value,
                RawOcrText: ocrResponse.RawText,
                ConfidenceScore: ocrResponse.ConfidenceScore,
                Message: $"Trích xuất thành công: {extractedWeight.Value} kg. Vui lòng xác nhận."
            );
        }
        catch (Exception ex) when (ex is not BadRequestException)
        {
            return new WeightOcrExtractResultDto(
                Success: false,
                ExtractedWeightKg: null,
                RawOcrText: null,
                ConfidenceScore: null,
                Message: $"Lỗi khi xử lý ảnh: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Parse weight value from OCR raw text using 4-tier regex.
    /// </summary>
    private static decimal? ParseWeightFromText(string text)
    {
        // Pattern 1: "65.5 kg" or "65.5kg"
        var match1 = Regex.Match(text, @"(\d{2,3}\.?\d{0,2})\s*kg", RegexOptions.IgnoreCase);
        if (match1.Success && decimal.TryParse(match1.Groups[1].Value, out var w1) && w1 >= 30 && w1 <= 200)
            return w1;

        // Pattern 2: "Weight: 65.5" or "Cân nặng: 65.5"
        var match2 = Regex.Match(text, @"(?:weight|cân\s*nặng|wt)[:\s]+(\d{2,3}\.?\d{0,2})", RegexOptions.IgnoreCase);
        if (match2.Success && decimal.TryParse(match2.Groups[1].Value, out var w2) && w2 >= 30 && w2 <= 200)
            return w2;

        // Pattern 3: Standalone decimal number in valid range
        var matches = Regex.Matches(text, @"\b(\d{2,3}\.\d{1,2})\b");
        foreach (Match m in matches)
        {
            if (decimal.TryParse(m.Groups[1].Value, out var w3) && w3 >= 30 && w3 <= 200)
                return w3;
        }

        // Pattern 4: Integer-only fallback
        var matches4 = Regex.Matches(text, @"\b(\d{2,3})\b");
        foreach (Match m in matches4)
        {
            if (decimal.TryParse(m.Groups[1].Value, out var w4) && w4 >= 30 && w4 <= 200)
                return w4;
        }

        return null;
    }
}
