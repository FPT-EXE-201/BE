using FPT.EXE201.Application.AI.Models;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.AI;

/// <summary>
/// Fluent builder cho AI prompts với Rule Layer system.
/// Lắp ráp: System Rules + Domain Rules + Feature Rules + Output Schema → SystemMessage
///           RAG Context + User Input → UserMessage
///
/// Usage (manual):
///   PromptBuilder.Create()
///     .WithSystemRules("...")
///     .WithDomainRules("...")
///     .WithFeatureRules("...")
///     .WithOutputSchema("...")
///     .WithContext("pregnancy", pregnancyJson)
///     .WithUserMessage(ocrText)
///     .Build();
///
/// Usage (from DB template):
///   PromptBuilder.FromTemplate(template)
///     .WithContext("pregnancy", pregnancyJson)
///     .WithUserMessage(ocrText)
///     .Build();
/// </summary>
public class PromptBuilder
{
    private readonly List<string> _systemParts = new();
    private readonly List<string> _contextParts = new();
    private string _userMessage = "";
    private string? _outputSchema;
    private string _modelName = "gemini-2.5-flash";
    private double _temperature = 0.1;
    private int _maxOutputTokens = 16384;
    private bool _jsonMode = true;

    public static PromptBuilder Create() => new();

    /// <summary>
    /// Khởi tạo PromptBuilder từ AiPromptTemplate (loaded from DB).
    /// Auto-fills System/Domain/Feature rules + model config.
    /// Caller chỉ cần thêm Context + UserMessage.
    /// </summary>
    public static PromptBuilder FromTemplate(AiPromptTemplate template)
    {
        var builder = new PromptBuilder();

        builder.WithSystemRules(template.SystemRules);

        if (!string.IsNullOrWhiteSpace(template.DomainRules))
            builder.WithDomainRules(template.DomainRules);

        builder.WithFeatureRules(template.FeatureRules);

        if (!string.IsNullOrWhiteSpace(template.OutputSchema))
            builder.WithOutputSchema(template.OutputSchema);

        builder._modelName = template.ModelName;
        builder._temperature = template.Temperature;
        builder._maxOutputTokens = template.MaxOutputTokens;

        return builder;
    }

    // ═══ Layer 1: System Rules ═══
    public PromptBuilder WithSystemRules(string rules)
    {
        _systemParts.Insert(0, $"[SYSTEM RULES]\n{rules}");
        return this;
    }

    // ═══ Layer 2: Domain Rules ═══
    public PromptBuilder WithDomainRules(string rules)
    {
        _systemParts.Add($"[DOMAIN KNOWLEDGE]\n{rules}");
        return this;
    }

    // ═══ Layer 3: Feature Rules ═══
    public PromptBuilder WithFeatureRules(string rules)
    {
        _systemParts.Add($"[TASK INSTRUCTIONS]\n{rules}");
        return this;
    }

    // ═══ Output Schema ═══
    public PromptBuilder WithOutputSchema(string schema)
    {
        _outputSchema = schema;
        return this;
    }

    // ═══ Layer 4: RAG Context (injected into user message) ═══
    public PromptBuilder WithContext(string label, string data)
    {
        _contextParts.Add($"[{label.ToUpperInvariant()}]\n{data}");
        return this;
    }

    // ═══ User Input ═══
    public PromptBuilder WithUserMessage(string message)
    {
        _userMessage = message;
        return this;
    }

    // ═══ Model Configuration ═══
    public PromptBuilder WithModel(string modelName)
    {
        _modelName = modelName;
        return this;
    }

    public PromptBuilder WithTemperature(double temperature)
    {
        _temperature = temperature;
        return this;
    }

    public PromptBuilder WithMaxTokens(int maxTokens)
    {
        _maxOutputTokens = maxTokens;
        return this;
    }

    public PromptBuilder WithJsonMode(bool enabled)
    {
        _jsonMode = enabled;
        return this;
    }

    // ═══ Build ═══
    public AiPrompt Build()
    {
        // Assemble system message from rule layers
        var systemParts = new List<string>(_systemParts);

        if (!string.IsNullOrWhiteSpace(_outputSchema))
        {
            systemParts.Add($"[OUTPUT JSON SCHEMA]\nYour response MUST be valid JSON conforming to this schema:\n{_outputSchema}");
        }

        var systemMessage = string.Join("\n\n", systemParts);

        // Assemble user message: RAG context + user input
        var userParts = new List<string>();

        if (_contextParts.Any())
        {
            userParts.Add("CONTEXT (from patient records):");
            userParts.AddRange(_contextParts);
            userParts.Add("---");
        }

        userParts.Add(_userMessage);

        var userMessage = string.Join("\n\n", userParts);

        return new AiPrompt(
            SystemMessage: systemMessage,
            UserMessage: userMessage,
            ModelName: _modelName,
            Temperature: _temperature,
            MaxOutputTokens: _maxOutputTokens,
            JsonMode: _jsonMode
        );
    }
}
