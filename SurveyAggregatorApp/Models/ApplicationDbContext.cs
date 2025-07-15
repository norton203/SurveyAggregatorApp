// Models/ApplicationDbContext.cs
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SurveyAggregatorApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<ProviderAccount> ProviderAccounts { get; set; }
        public DbSet<CompletedSurvey> CompletedSurveys { get; set; }
        public DbSet<SurveyProvider> SurveyProviders { get; set; }
        public DbSet<UserTransaction> UserTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // User entity configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.TotalEarnings).HasPrecision(10, 2);

                // One-to-one relationship with UserProfile
                entity.HasOne(e => e.Profile)
                    .WithOne(p => p.User)
                    .HasForeignKey<UserProfile>(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // One-to-many relationships
                entity.HasMany(e => e.ConnectedAccounts)
                    .WithOne(p => p.User)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.CompletedSurveys)
                    .WithOne(s => s.User)
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Transactions)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // UserProfile entity configuration
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.Country).HasMaxLength(50);
                entity.Property(e => e.City).HasMaxLength(100);
                entity.Property(e => e.PostalCode).HasMaxLength(20);
            });

            // ProviderAccount entity configuration
            modelBuilder.Entity<ProviderAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.EarningsFromProvider).HasPrecision(10, 2);
                entity.HasIndex(e => new { e.UserId, e.ProviderId }).IsUnique();
            });

            // CompletedSurvey entity configuration
            modelBuilder.Entity<CompletedSurvey>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.SurveyId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.ProviderId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Reward).HasPrecision(10, 2);
                entity.Property(e => e.Status).HasMaxLength(50);
            });

            // SurveyProvider entity configuration
            modelBuilder.Entity<SurveyProvider>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LogoUrl).HasMaxLength(255);
                entity.Property(e => e.ApiEndpoint).HasMaxLength(255);
                entity.Property(e => e.AuthUrl).HasMaxLength(255);
                entity.Property(e => e.MinPayout).HasPrecision(10, 2);
            });

            // UserTransaction entity configuration
            modelBuilder.Entity<UserTransaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Amount).HasPrecision(10, 2);
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Status).HasMaxLength(50);
                entity.Property(e => e.Reference).HasMaxLength(100);
            });

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed survey providers
            modelBuilder.Entity<SurveyProvider>().HasData(
                new SurveyProvider
                {
                    Id = "pollfish",
                    Name = "Pollfish",
                    LogoUrl = "/images/pollfish-logo.png",
                    Description = "Real-time survey platform with instant rewards",
                    ApiEndpoint = "https://api.pollfish.com/v2/",
                    AuthUrl = "https://www.pollfish.com/oauth/authorize",
                    MinPayout = 0.30m,
                    IsActive = true
                },
                new SurveyProvider
                {
                    Id = "dynata",
                    Name = "Dynata",
                    LogoUrl = "/images/dynata-logo.png",
                    Description = "Leading market research and data platform",
                    ApiEndpoint = "https://api.dynata.com/v1/",
                    AuthUrl = "https://portal.dynata.com/oauth/authorize",
                    MinPayout = 0.50m,
                    IsActive = true
                },
                new SurveyProvider
                {
                    Id = "lucid",
                    Name = "Lucid (Cint)",
                    LogoUrl = "/images/lucid-logo.png",
                    Description = "Sample marketplace for market research",
                    ApiEndpoint = "https://api.luc.id/v1/",
                    AuthUrl = "https://suppliers.luc.id/oauth/authorize",
                    MinPayout = 0.25m,
                    IsActive = true
                },
                new SurveyProvider
                {
                    Id = "surveymonkey",
                    Name = "SurveyMonkey Audience",
                    LogoUrl = "/images/surveymonkey-logo.png",
                    Description = "Survey creation and audience platform",
                    ApiEndpoint = "https://api.surveymonkey.com/v3/",
                    AuthUrl = "https://api.surveymonkey.com/oauth/authorize",
                    MinPayout = 1.00m,
                    IsActive = true
                }
            );
        }
    }
}