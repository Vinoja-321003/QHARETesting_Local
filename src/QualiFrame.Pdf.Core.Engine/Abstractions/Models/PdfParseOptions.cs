namespace QualiFrame.Pdf.Core.Engine.Abstractions.Models;

/// <summary>Options for parsing: depth and page range.</summary>
public sealed class PdfParseOptions
{
    public bool IncludeStructure { get; init; } = true;
    public bool IncludeTables { get; init; } = true;
    public bool IncludeImages { get; init; } = true;
    public bool IncludeFontInfo { get; init; } = true;
    public bool IncludeMetadata { get; init; } = true;
    public int? MaxPages { get; init; }
    public int? StartPage { get; init; }
}
