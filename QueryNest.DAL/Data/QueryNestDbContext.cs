using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using QueryNest.Domain.Entities;

namespace QueryNest.DAL.Data;

public class QueryNestDbContext : IdentityDbContext<IdentityUser>
{
    public QueryNestDbContext(DbContextOptions<QueryNestDbContext> options) : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Vote> Votes => Set<Vote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.UserId);

            entity.Property(x => x.AspNetUserId).HasMaxLength(450).IsRequired();
            entity.HasIndex(x => x.AspNetUserId).IsUnique();

            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).HasPrecision(3);

            entity.HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<UserProfile>(x => x.AspNetUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.CategoryId);

            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();

            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasPrecision(3);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.ToTable("Tags");
            entity.HasKey(x => x.TagId);

            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(50).IsRequired();
            entity.HasIndex(x => x.Slug).IsUnique();

            entity.Property(x => x.CreatedAt).HasPrecision(3);
        });

        builder.Entity<Question>(entity =>
        {
            entity.ToTable("Questions");
            entity.HasKey(x => x.QuestionId);

            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.ViewCount).HasDefaultValue(0);
            entity.Property(x => x.CreatedAt).HasPrecision(3);
            entity.Property(x => x.UpdatedAt).HasPrecision(3);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Questions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<QuestionTag>(entity =>
        {
            entity.ToTable("QuestionTags");
            entity.HasKey(x => new { x.QuestionId, x.TagId });

            entity.HasOne(x => x.Question)
                .WithMany(x => x.QuestionTags)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Tag)
                .WithMany(x => x.QuestionTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Answer>(entity =>
        {
            entity.ToTable("Answers");
            entity.HasKey(x => x.AnswerId);

            entity.Property(x => x.Content).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.CreatedAt).HasPrecision(3);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Answers)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(x => x.CommentId);

            entity.Property(x => x.Content).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.CreatedAt).HasPrecision(3);

            entity.HasOne(x => x.Answer)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ParentComment)
                .WithMany(x => x.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Vote>(entity =>
        {
            entity.ToTable("Votes");
            entity.HasKey(x => x.VoteId);

            entity.Property(x => x.VoteType).HasConversion<int>().IsRequired();
            entity.Property(x => x.CreatedAt).HasPrecision(3);

            entity.HasOne(x => x.User)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Question)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Answer)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.Comment)
                .WithMany(x => x.Votes)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.UserId, x.QuestionId })
                .IsUnique()
                .HasFilter("[QuestionId] IS NOT NULL");

            entity.HasIndex(x => new { x.UserId, x.AnswerId })
                .IsUnique()
                .HasFilter("[AnswerId] IS NOT NULL");

            entity.HasIndex(x => new { x.UserId, x.CommentId })
                .IsUnique()
                .HasFilter("[CommentId] IS NOT NULL");

            entity.ToTable(t =>
            {
                t.HasCheckConstraint(
                    "CK_Votes_ExactlyOneTarget",
                    "([QuestionId] IS NOT NULL AND [AnswerId] IS NULL AND [CommentId] IS NULL) OR " +
                    "([QuestionId] IS NULL AND [AnswerId] IS NOT NULL AND [CommentId] IS NULL) OR " +
                    "([QuestionId] IS NULL AND [AnswerId] IS NULL AND [CommentId] IS NOT NULL)"
                );

                t.HasCheckConstraint(
                    "CK_Votes_VoteType_UpOrDown",
                    "[VoteType] IN (-1, 1)"
                );
            });
        });
    }
}
