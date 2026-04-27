using HotelManagementSystem.Shared.DTOs;
using HotelManagementSystem.Shared.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
#pragma warning disable CS8600, CS8602

namespace HotelManagementSystem.Shared.Utilities
{
    /// <summary>
    /// Generates a branded PDF invoice using QuestPDF.
    /// Company branding (logo, colours, font) is supplied via the <see cref="CompanyProfile"/>
    /// entity that lives in the database.  All PDF generation is server-side — never client-side.
    /// </summary>
    public static class InvoicePdfUtility
    {
        // ── Defaults used when no CompanyProfile is configured ─────────────────
        private const string DefaultPrimary = "#1a73e8";
        private const string DefaultAccent  = "#f5f5f5";
        private const string DefaultFont    = "Arial";

        /// <summary>
        /// Generates a PDF for the supplied invoice and returns the raw bytes.
        /// </summary>
        /// <param name="invoice">Mapped DTO — must include <see cref="InvoiceDTO.Items"/>.</param>
        /// <param name="profile">Optional company branding profile.  Pass <c>null</c> for defaults.</param>
        public static byte[] Generate(InvoiceDTO invoice, CompanyProfile? profile)
        {
            // QuestPDF community licence — free for open-source / development use.
            QuestPDF.Settings.License = LicenseType.Community;

            var primaryHex = profile?.PrimaryColor ?? DefaultPrimary;
            var accentHex  = profile?.AccentColor  ?? DefaultAccent;
            var fontFamily = profile?.FontFamily   ?? DefaultFont;

            var primary = NormalizeHex(primaryHex);
            var accent  = NormalizeHex(accentHex);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(t => t.FontFamily(fontFamily).FontSize(10));

                    // ── Header ──────────────────────────────────────────────────
                    page.Header().Element(header =>
                    {
                        header.Background(primary).Padding(16).Row(row =>
                        {
                            // Logo (optional)
                            if (!string.IsNullOrWhiteSpace(profile?.LogoUrl)
                                && File.Exists(profile.LogoUrl))
                            {
                                row.ConstantItem(80).Image(profile.LogoUrl);
                            }

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text(profile?.CompanyName ?? "Hotel")
                                   .Bold().FontSize(18).FontColor(Colors.White);

                                if (!string.IsNullOrWhiteSpace(profile?.Address))
                                    col.Item().Text(profile.Address).FontColor(Colors.White);

                                if (!string.IsNullOrWhiteSpace(profile?.GstinNumber))
                                    col.Item().Text($"GSTIN: {profile.GstinNumber}").FontColor(Colors.White);

                                if (!string.IsNullOrWhiteSpace(profile?.PhoneNumber))
                                    col.Item().Text($"Tel: {profile.PhoneNumber}").FontColor(Colors.White);

                                if (!string.IsNullOrWhiteSpace(profile?.Email))
                                    col.Item().Text(profile.Email).FontColor(Colors.White);

                                if (!string.IsNullOrWhiteSpace(profile?.Website))
                                    col.Item().Text(profile.Website).FontColor(Colors.White);
                            });

                            row.ConstantItem(120).Column(col =>
                            {
                                col.Item().AlignRight().Text("INVOICE")
                                   .Bold().FontSize(22).FontColor(Colors.White);
                                col.Item().AlignRight().Text($"# {invoice.Id}")
                                   .FontColor(Colors.White);
                            });
                        });
                    });

                    // ── Content ─────────────────────────────────────────────────
                    page.Content().PaddingVertical(16).Column(col =>
                    {
                        // Invoice meta
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Bill To: {invoice.GuestName}").Bold();
                                c.Item().Text($"Room: {invoice.RoomNumber}");
                                c.Item().Text($"Booking ID: {invoice.BookingId}");
                            });

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Issue Date: {invoice.IssuedDate:dd MMM yyyy}");
                                c.Item().AlignRight().Text($"Due Date:   {invoice.DueDate:dd MMM yyyy}");
                                c.Item().AlignRight().Text($"Status: {invoice.Status}").Bold();
                            });
                        });

                        col.Item().PaddingVertical(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        // Line-items table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(5);  // Description
                                cols.RelativeColumn(1);  // Qty
                                cols.RelativeColumn(2);  // Unit Price
                                cols.RelativeColumn(2);  // Total
                            });

                            // Table header row
                            table.Header(h =>
                            {
                                h.Cell().Background(primary).Padding(6)
                                 .Text("Description").Bold().FontColor(Colors.White);
                                h.Cell().Background(primary).Padding(6)
                                 .AlignCenter().Text("Qty").Bold().FontColor(Colors.White);
                                h.Cell().Background(primary).Padding(6)
                                 .AlignRight().Text("Unit Price").Bold().FontColor(Colors.White);
                                h.Cell().Background(primary).Padding(6)
                                 .AlignRight().Text("Total").Bold().FontColor(Colors.White);
                            });

                            // Data rows — alternate accent / white
                            bool odd = false;
                            foreach (var item in invoice.Items)
                            {
                                var rowBg = odd ? accent : "#FFFFFF";
                                odd = !odd;

                                var unitPrice = item.Quantity > 0
                                    ? item.Amount / item.Quantity
                                    : item.Amount;

                                table.Cell().Background(rowBg).Padding(5).Text(item.Description);
                                table.Cell().Background(rowBg).Padding(5).AlignCenter().Text(item.Quantity.ToString());
                                table.Cell().Background(rowBg).Padding(5).AlignRight().Text($"{unitPrice:C2}");
                                table.Cell().Background(rowBg).Padding(5).AlignRight().Text($"{item.Amount:C2}");
                            }
                        });

                        // Total
                        col.Item().PaddingTop(8).AlignRight()
                           .Text($"Grand Total: {invoice.TotalAmount:C2}").Bold().FontSize(12);
                    });

                    // ── Footer ──────────────────────────────────────────────────
                    page.Footer().AlignCenter()
                        .Text(t =>
                        {
                            t.Span("Thank you for your business   |   Page ");
                            t.CurrentPageNumber();
                            t.Span(" of ");
                            t.TotalPages();
                        });
                });
            }).GeneratePdf();
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static string NormalizeHex(string hex)
        {
            var clean = hex.TrimStart('#');
            return clean.Length == 6 ? $"#{clean.ToUpperInvariant()}" : DefaultPrimary;
        }
    }
}
