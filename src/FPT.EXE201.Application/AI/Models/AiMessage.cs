namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Message trong multi-turn conversation (dùng cho chat feature — Nutrition Week sau).
/// </summary>
public record AiMessage(
    /// <summary>"user" hoặc "model" (Gemini convention).</summary>
    string Role,

    /// <summary>Nội dung message.</summary>
    string Content
);
