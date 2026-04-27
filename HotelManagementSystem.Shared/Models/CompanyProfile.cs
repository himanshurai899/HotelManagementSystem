namespace HotelManagementSystem.Shared.Models
{
    // CompanyProfile.cs
    // Stores invoice branding configuration for the hotel company.
    // This is a single-row settings table — only one record should ever exist.
    // NOTE: In Phase 9 (Multi-Tenant SaaS), each Tenant will own its own
    //       CompanyProfile (TenantId FK will be added at that stage).
    public class CompanyProfile
    {
        public int Id { get; set; }

        // ── Identity ───────────────────────────────────────────────────────────
        public string CompanyName { get; set; } = string.Empty;
        public string? LogoUrl { get; set; }    // Stored at uploads/{CompanyName}/assets/

        // ── Contact ───────────────────────────────────────────────────────────
        public string? Address { get; set; }
        public string? GstinNumber { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }

        // ── PDF Branding (used by InvoicePdfUtility) ──────────────────────────
        public string? PrimaryColor { get; set; }  // Hex e.g. "#1a73e8" — PDF header bg
        public string? AccentColor { get; set; }   // Hex e.g. "#f5f5f5" — PDF alt row bg
        public string? FontFamily { get; set; }    // e.g. "Arial", "Helvetica"
    }
}
