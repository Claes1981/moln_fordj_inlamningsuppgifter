namespace CloudSoft.Web.Utilities;

/// <summary>
/// Validates that a file stream is a valid PDF by checking its magic bytes.
/// PDF files start with "%PDF" (hex: 25 50 44 46).
/// </summary>
public static class PdfValidation
{
    private static readonly byte[] PdfMagicBytes = [0x25, 0x50, 0x44, 0x46]; // "%PDF"

    public static bool IsPdf(Stream stream)
    {
        if (stream.Length < PdfMagicBytes.Length)
            return false;

        stream.Position = 0;
        var header = new byte[PdfMagicBytes.Length];
        stream.ReadExactly(header, 0, PdfMagicBytes.Length);
        stream.Position = 0;

        return header.SequenceEqual(PdfMagicBytes);
    }
}
