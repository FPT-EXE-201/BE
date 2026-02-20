using FPT.EXE201.Application.AI.Models;

namespace FPT.EXE201.Application.AI.Interfaces;

/// <summary>
/// Abstraction cho AI model provider (Gemini, OpenAI, etc.).
/// Week 5: implement GenerateAsync cho single completion.
/// GenerateAsync dùng cho: extraction, meal planning, summarization.
/// ChatAsync dùng cho: nutrition chat (Week sau).
/// </summary>
public interface IAiProvider
{
    /// <summary>
    /// Single completion — gửi prompt, nhận 1 response.
    /// Dùng cho extraction, planning, summarization.
    /// </summary>
    Task<AiResponse> GenerateAsync(AiPrompt prompt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Multi-turn conversation — gửi lịch sử messages, nhận response tiếp theo.
    /// Dùng cho chat feature (Nutrition Week sau).
    /// Pass -1 for temperature/maxOutputTokens to use appsettings defaults.
    /// </summary>
    Task<AiResponse> ChatAsync(
        List<AiMessage> messages,
        string systemMessage,
        string modelName,
        double temperature = -1,
        int maxOutputTokens = -1,
        CancellationToken cancellationToken = default);
}
