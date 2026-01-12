using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FPT.EXE201.Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } // set ở Infrastructure (SaveChanges)
        public DateTime UpdatedAt { get; set; } // set ở Infrastructure (SaveChanges)
        public DateTime? DeletedAt { get; set; }

        public bool IsDeleted => DeletedAt != null; // KHÔNG map DB
    }
}

