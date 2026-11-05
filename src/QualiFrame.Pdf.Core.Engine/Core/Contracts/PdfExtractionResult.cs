namespace QualiFrame.Pdf.Core.Engine.Core.Contracts;

/// <summary>
/// Result of PDF content extraction (text, structure, images) for validation or comparison.
/// </summary>
public class PdfExtractionResult
{
    public string SourcePath { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public IReadOnlyList<PdfPageContent> Pages { get; set; } = Array.Empty<PdfPageContent>();
    public PdfDocumentMetadata? Metadata { get; set; }
}

/// <summary>
/// Per-page extracted content.
/// </summary>
public class PdfPageContent
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
    public IReadOnlyList<PdfTextBlock> TextBlocks { get; set; } = Array.Empty<PdfTextBlock>();
    public IReadOnlyList<PdfImageInfo> Images { get; set; } = Array.Empty<PdfImageInfo>();
    public IReadOnlyList<PdfTableInfo>? Tables { get; set; }
}

/// <summary>
/// Text with bounding box for layout-aware comparison.
/// </summary>
public class PdfTextBlock
{
    public string Text { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string? FontName { get; set; }
    public double? FontSize { get; set; }
    public bool Bold { get; set; }
    public bool Italic { get; set; }
}

/// <summary>
/// Embedded image info for presence/position/dimension validation.
/// </summary>
public class PdfImageInfo
{
    public int PageNumber { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int? PixelWidth { get; set; }
    public int? PixelHeight { get; set; }
    public string? FingerprintHash { get; set; }
}

/// <summary>
/// Extracted table structure for table validation.
/// </summary>
public class PdfTableInfo
{
    public int PageNumber { get; set; }
    public int StartPage { get; set; }
    public int EndPage { get; set; }
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public IReadOnlyList<string>? HeaderCells { get; set; }
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; set; } = Array.Empty<IReadOnlyList<string>>();
    public PdfBoundingBox? Bounds { get; set; }
}

/// <summary>
/// Bounding box in PDF coordinates.
/// </summary>
public class PdfBoundingBox
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

/// <summary>
/// Document-level metadata.
/// </summary>
public class PdfDocumentMetadata
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Producer { get; set; }
    public DateTime? CreationDate { get; set; }
    public DateTime? ModDate { get; set; }
}

