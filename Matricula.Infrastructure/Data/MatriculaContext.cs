using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Matricula.Infrastructure.Data
{
    public class MatriculaContext : DbContext
    {
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<ClassRoomCourse> ClassRoomCourses { get; set; }
        public DbSet<EnrollmentDetail> EnrollmentDetails { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<ClassRoom> ClassRooms { get; set; }
        public DbSet<AdminUser> AdminUsers { get; set; }

        public MatriculaContext(){}

        public MatriculaContext(DbContextOptions<MatriculaContext> options) : base(options) { }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseNpgsql(ConnectionStringFactory.FromEnvironment());
            }
        }

        public override int SaveChanges()
        {
            TouchUpdatedAt();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            TouchUpdatedAt();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void TouchUpdatedAt()
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>().Where(e => e.State == EntityState.Modified))
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Enrollment>().HasIndex(e => new { e.StudentId, e.Period }).IsUnique();
            modelBuilder.Entity<AdminUser>().HasIndex(a => a.Username).IsUnique();
            modelBuilder.Entity<EnrollmentDetail>().HasIndex(d => new { d.EnrollmentId, d.ClassRoomCourseId }).IsUnique();
        }
    }
}
