namespace AeroMech.Models.Models
{
    /// <summary>
    /// A supplier code to receive against, with how many parts carry it. Built by grouping the
    /// parts themselves, because supplier codes are held on <c>Part.SupplierCode</c> and there is
    /// no supplier record to list.
    /// </summary>
    public class SupplierOptionModel
    {
        public string SupplierCode { get; set; } = string.Empty;
        public int PartCount { get; set; }
    }
}
