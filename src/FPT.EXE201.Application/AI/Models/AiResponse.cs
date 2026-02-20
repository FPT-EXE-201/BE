namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Response từ AI provider.
/// </summary>
public record AiResponse(
    /// <summary>Nội dung text response từ AI.</summary>
    string Content,

    /// <summary>Số tokens input (prompt).</summary>
    int PromptTokens,

    /// <summary>Số tokens output (completion).</summary>
    int CompletionTokens,

    /// <summary>Tổng tokens (prompt + completion).</summary>
    int TotalTokens,

    /// <summary>Model thực tế đã xử lý.</summary>
    string ModelUsed,

    /// <summary>Thời gian xử lý.</summary>
    TimeSpan ProcessingTime
);
