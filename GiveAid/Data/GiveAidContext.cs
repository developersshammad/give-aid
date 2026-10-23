using GiveAid.Models;
using Microsoft.EntityFrameworkCore;

namespace GiveAid.Data
{
    /// <summary>
    /// Entity Framework Core database context. Every DbSet below becomes one
    /// table in the SQL Server database "GiveAidDB".
    /// </summary>
    public class GiveAidContext : DbContext
    {
        public GiveAidContext(DbContextOptions<GiveAidContext> options) : base(options) { }

        public DbSet<Admin> Admins => Set<Admin>();
        public DbSet<Member> Members => Set<Member>();
        public DbSet<Cause> Causes => Set<Cause>();
        public DbSet<Donation> Donations => Set<Donation>();
        public DbSet<Partner> Partners => Set<Partner>();
        public DbSet<Ngo> Ngos => Set<Ngo>();
        public DbSet<Programme> Programmes => Set<Programme>();
        public DbSet<ProgrammeInterest> ProgrammeInterests => Set<ProgrammeInterest>();
        public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
        public DbSet<SupportQuery> Queries => Set<SupportQuery>();
        public DbSet<Invitation> Invitations => Set<Invitation>();
        public DbSet<ContentPage> ContentPages => Set<ContentPage>();
        public DbSet<ProgrammeCostLine> ProgrammeCostLines => Set<ProgrammeCostLine>();
        public DbSet<PlannerSession> PlannerSessions => Set<PlannerSession>();
        public DbSet<PlannerStep> PlannerSteps => Set<PlannerStep>();
        public DbSet<EmailOtp> EmailOtps => Set<EmailOtp>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names (same as the SQL script in /Database)
            modelBuilder.Entity<Admin>().ToTable("Admins");
            modelBuilder.Entity<Member>().ToTable("Members");
            modelBuilder.Entity<EmailOtp>().ToTable("EmailOtps");
            modelBuilder.Entity<EmailOtp>().HasIndex(o => new { o.Email, o.Purpose });
            modelBuilder.Entity<Cause>().ToTable("Causes");
            modelBuilder.Entity<Donation>().ToTable("Donations");
            modelBuilder.Entity<Partner>().ToTable("Partners");
            modelBuilder.Entity<Ngo>().ToTable("Ngos");
            modelBuilder.Entity<Programme>().ToTable("Programmes");
            modelBuilder.Entity<ProgrammeInterest>().ToTable("ProgrammeInterests");
            modelBuilder.Entity<GalleryImage>().ToTable("GalleryImages");
            modelBuilder.Entity<SupportQuery>().ToTable("Queries");
            modelBuilder.Entity<Invitation>().ToTable("Invitations");
            modelBuilder.Entity<ContentPage>().ToTable("ContentPages");
            modelBuilder.Entity<ProgrammeCostLine>().ToTable("ProgrammeCostLines");
            modelBuilder.Entity<PlannerSession>().ToTable("PlannerSessions");
            modelBuilder.Entity<PlannerStep>().ToTable("PlannerSteps");

            // Unique values
            modelBuilder.Entity<Admin>().HasIndex(a => a.Username).IsUnique();
            modelBuilder.Entity<Member>().HasIndex(m => m.Email).IsUnique();
            modelBuilder.Entity<Donation>().HasIndex(d => d.TransactionRef).IsUnique();
            modelBuilder.Entity<ContentPage>().HasIndex(p => p.Slug).IsUnique();

            // ----- Relationships & delete rules -----
            // A cause that already has donations can NOT be deleted (keeps payment history safe).
            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Cause).WithMany().HasForeignKey(d => d.CauseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Optional links: the application sets these to NULL before deleting the parent.
            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Member).WithMany().HasForeignKey(d => d.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Programme>()
                .HasOne(p => p.Ngo).WithMany().HasForeignKey(p => p.NgoId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<GalleryImage>()
                .HasOne(g => g.Programme).WithMany().HasForeignKey(g => g.ProgrammeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<SupportQuery>()
                .HasOne(q => q.Member).WithMany().HasForeignKey(q => q.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ProgrammeInterest>()
                .HasOne(i => i.Member).WithMany().HasForeignKey(i => i.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // AI Programme Planner
            modelBuilder.Entity<ProgrammeCostLine>()
                .HasOne(c => c.Programme).WithMany().HasForeignKey(c => c.ProgrammeId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PlannerSession>()
                .HasOne(s => s.Programme).WithMany().HasForeignKey(s => s.ProgrammeId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PlannerStep>()
                .HasOne(s => s.Session).WithMany().HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Interests & invitations are removed together with their parent.
            modelBuilder.Entity<ProgrammeInterest>()
                .HasOne(i => i.Programme).WithMany().HasForeignKey(i => i.ProgrammeId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Invitation>()
                .HasOne(i => i.Member).WithMany().HasForeignKey(i => i.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
