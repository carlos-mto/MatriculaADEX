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
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

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
            modelBuilder.Entity<Teacher>().HasIndex(t => t.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            modelBuilder.Entity<AppSetting>().HasIndex(a => a.Key).IsUnique();
            modelBuilder.Entity<ClassRoomCourse>().HasIndex(c => new { c.Period, c.ClassRoomId });
            modelBuilder.Entity<ClassRoomCourse>().HasIndex(c => new { c.Period, c.TeacherId });

            modelBuilder.Entity<Enrollment>().HasOne(e => e.Student).WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Enrollment>().HasOne(e => e.Shift).WithMany().HasForeignKey(e => e.ShiftId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EnrollmentDetail>().HasOne(d => d.ClassRoomCourse).WithMany().HasForeignKey(d => d.ClassRoomCourseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<EnrollmentDetail>().HasOne(d => d.Enrollment).WithMany().HasForeignKey(d => d.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ClassRoomCourse>().HasOne(c => c.Course).WithMany().HasForeignKey(c => c.CourseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ClassRoomCourse>().HasOne(c => c.ClassRoom).WithMany().HasForeignKey(c => c.ClassRoomId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ClassRoomCourse>().HasOne(c => c.Shift).WithMany().HasForeignKey(c => c.ShiftId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ClassRoomCourse>().HasOne(c => c.Teacher).WithMany().HasForeignKey(c => c.TeacherId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
