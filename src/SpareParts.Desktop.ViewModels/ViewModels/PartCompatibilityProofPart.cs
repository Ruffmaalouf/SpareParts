namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PartCompatibilityProofPart
    {
        public PartCompatibilityProofPart(
            PartCompatibilityPartRow part,
            string reason,
            int score)
        {
            Part = part;
            Reason = reason;
            ScoreText = $"{score}%";
        }

        public PartCompatibilityPartRow Part { get; }
        public string Reason { get; }
        public string ScoreText { get; }
    }
}
