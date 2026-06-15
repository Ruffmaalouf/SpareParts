namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartCompatibilityFitmentGroup
    {
        public string ModelName { get; init; } = string.Empty;
        public string YearRange { get; init; } = string.Empty;
        public int PartCount { get; init; }
        public int DonorCount { get; init; }
        public string ReasonText { get; init; } = string.Empty;
        public string ProofCodes { get; init; } = string.Empty;
        public string PartCountText => $"{PartCount:N0} matching parts";
        public string DonorCountText => $"{DonorCount:N0} donor cars";
    }
}
