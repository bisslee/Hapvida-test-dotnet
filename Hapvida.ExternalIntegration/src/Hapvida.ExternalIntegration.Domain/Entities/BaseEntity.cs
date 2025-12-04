using Hapvida.ExternalIntegration.Domain.Entities.Enums;
using System;

namespace Hapvida.ExternalIntegration.Domain.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = "System";
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; } = "System";

        public DataStatus Status { get; set; } = DataStatus.Created;
    }
}

