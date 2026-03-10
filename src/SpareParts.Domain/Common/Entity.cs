namespace SpareParts.Domain.Common
{
    public abstract class Entity
    {
        public int Id { get; set; }
    }

    public abstract class AuditableEntity : Entity
    {
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedByUserId { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByUserId { get; set; }
    }
}
