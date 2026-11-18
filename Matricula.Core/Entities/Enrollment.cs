using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Matricula.Core.Entities;

public class Enrollment : BaseEntity
{
    [Required, MaxLength(20)]
    public string Period { get; set; } = "";

    public Guid StudentId { get; set; }
    [ForeignKey("StudentId")]
    public Student Student { get; set; }

    public Guid? ShiftId { get; set; }
    [ForeignKey("ShiftId")]
    public Shift? Shift { get; set; }
}
