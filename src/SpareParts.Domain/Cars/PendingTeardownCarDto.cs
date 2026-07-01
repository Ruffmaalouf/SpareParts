namespace SpareParts.Domain.Cars
{
    /// <summary>
    /// A used car the Intake &amp; Teardown Agent has not yet generated a parts/teardown plan for.
    /// </summary>
    public sealed class PendingTeardownCarDto
    {
        public int Id { get; set; }
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public int? CreatedByUserId { get; set; }
    }
}
