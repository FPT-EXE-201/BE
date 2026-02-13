namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Loại thai. Ảnh hưởng đến dinh dưỡng, theo dõi cân nặng và mức độ rủi ro.
/// </summary>
public enum PregnancyType
{
    /// <summary>Đơn thai — 1 em bé</summary>
    Singleton,

    /// <summary>Song thai — 2 em bé</summary>
    Twins,

    /// <summary>Tam thai — 3 em bé</summary>
    Triplets,

    /// <summary>Khác (đa thai > 3)</summary>
    Other
}
