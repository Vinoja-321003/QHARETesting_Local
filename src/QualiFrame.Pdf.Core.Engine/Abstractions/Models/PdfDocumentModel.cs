namespace QualiFrame.Pdf.Core.Engine.Abstractions.Models;

/// <summary>
/// Normalized document tree: pages → blocks → lines → spans, plus tables, images, metadata.
/// Immutable snapshot for validators.
/// </summary>
public sealed class PdfDocumentModel
{
    public string SourcePath { get; init; } = string.Empty;
    public int PageCount { get; init; }
    public IReadOnlyList<PdfPageModel> Pages { get; init; } = Array.Empty<PdfPageModel>();
    public PdfDocumentMetadataModel? Metadata { get; init; }
}

/// <summary>Single page in the document model.</summary>
public sealed class PdfPageModel
{
    public int PageNumber { get; init; }
    public IReadOnlyList<PdfBlockModel> Blocks { get; init; } = Array.Empty<PdfBlockModel>();
    public IReadOnlyList<PdfTableModel> Tables { get; init; } = Array.Empty<PdfTableModel>();
    public IReadOnlyList<PdfImageModel> Images { get; init; } = Array.Empty<PdfImageModel>();
}

/// <summary>Block (e.g. paragraph) containing lines.</summary>
public sealed class PdfBlockModel
{
    public PdfBoundingBox? Bounds { get; init; }
    public IReadOnlyList<PdfLineModel> Lines { get; init; } = Array.Empty<PdfLineModel>();
}

/// <summary>Line within a block, containing spans.</summary>
public sealed class PdfLineModel
{
    public PdfBoundingBox? Bounds { get; init; }
    public IReadOnlyList<PdfSpanModel> Spans { get; init; } = Array.Empty<PdfSpanModel>();
}

/// <summary>Text span with font and style.</summary>
public sealed class PdfSpanModel
{
    public string Text { get; init; } = string.Empty;
    public string? FontName { get; init; }
    public double? FontSize { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool SubsetFont { get; init; }
    public PdfBoundingBox? Bounds { get; init; }
}

/// <summary>Table structure.</summary>
public sealed class PdfTableModel
{
    public int PageNumber { get; init; }
    public int RowCount { get; init; }
    public int ColumnCount { get; init; }
    public IReadOnlyList<string>? HeaderCells { get; init; }
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();
    public PdfBoundingBox? Bounds { get; init; }
}

/// <summary>Image on a page.</summary>
public sealed class PdfImageModel
{
    public int PageNumber { get; init; }
    public PdfBoundingBox? Bounds { get; init; }
    public int? PixelWidth { get; init; }
    public int? PixelHeight { get; init; }
}

/// <summary>Document-level metadata.</summary>
public sealed class PdfDocumentMetadataModel
{
    public string? Title { get; init; }
    public string? Author { get; init; }
    public string? Producer { get; init; }
    public DateTime? CreationDate { get; init; }
    public DateTime? ModDate { get; init; }
}
