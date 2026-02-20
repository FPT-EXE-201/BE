using System.Text.Json.Serialization;
using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.AI.ExtractionModels;

/// <summary>
/// Top-level kết quả trích xuất từ medical record.
/// vitalsData maps 1:1 với VitalsJsonDto → serialize trực tiếp thành PrenatalVisit.VitalsJson.
/// </summary>
public class MedicalRecordExtractionResult
{
    /// <summary>
    /// Dữ liệu phiếu khám thai, compatible 1:1 với VitalsJsonDto.
    /// Sẽ được serialize trực tiếp thành PrenatalVisit.VitalsJson trong Week 5.5 confirm flow.
    /// </summary>
    [JsonPropertyName("vitalsData")]
    public VitalsJsonDto? VitalsData { get; set; }

    /// <summary>
    /// AI's overall confidence score for the extraction (0.0 - 1.0).
    /// </summary>
    [JsonPropertyName("overallConfidence")]
    public double OverallConfidence { get; set; }
}
