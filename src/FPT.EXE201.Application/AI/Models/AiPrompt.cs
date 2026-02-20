namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Prompt đã lắp ráp hoàn chỉnh, sẵn sàng gửi tới AI provider.
/// SystemMessage = tổng hợp từ các Rule Layers.
/// UserMessage = RAG context + user input.
/// </summary>
public record AiPrompt(
    /// <summary>System message: lắp ráp từ Layer 1 + 2 + 3 + OutputSchema.</summary>
    string SystemMessage,

    /// <summary>User message: RAG context + actual user input (OCR text, question, etc.).</summary>
    string UserMessage,

    /// <summary>Tên model AI (e.g., "gemini-2.5-flash").</summary>
    string ModelName,

    /// <summary>Temperature: 0.0 = deterministic, 1.0 = creative.</summary>
    double Temperature = 0.1,

    /// <summary>Max output tokens.</summary>
    int MaxOutputTokens = 8192,

    /// <summary>Yêu cầu AI trả JSON (responseMimeType = application/json).</summary>
    bool JsonMode = true
);
