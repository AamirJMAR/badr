using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Models;

namespace MyWebApp.Data
{
    public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }   // ✅ AJOUT
    public DbSet<Project> Projects { get; set; }
    public DbSet<Deliverable> Deliverables { get; set; }   
    public DbSet<EmailLog> EmailLogs { get; set; }   // ✅ AJOUT
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<TaskItem>()
        .HasOne(t => t.Project)
        .WithMany()
        .HasForeignKey(t => t.ProjectId)
        .OnDelete(DeleteBehavior.Cascade);

    modelBuilder.Entity<Deliverable>()
        .HasOne(d => d.Project)
        .WithMany()
        .HasForeignKey(d => d.ProjectId)
        .OnDelete(DeleteBehavior.Cascade);
}
}

}
