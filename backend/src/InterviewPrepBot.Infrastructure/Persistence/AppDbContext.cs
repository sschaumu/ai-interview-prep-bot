using Microsoft.EntityFrameworkCore;

namespace InterviewPrepBot.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<QuestionEntity> Questions => Set<QuestionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<QuestionEntity>(e =>
        {
            e.ToTable("question");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Text).HasColumnName("text").IsRequired();
            e.Property(x => x.NormalizedText).HasColumnName("normalized_text").IsRequired();
            e.Property(x => x.ReferenceAnswer).HasColumnName("reference_answer");
            e.Property(x => x.Embedding).HasColumnName("embedding");
            e.Property(x => x.EmbeddingProfile).HasColumnName("embedding_profile");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasIndex(x => x.NormalizedText).IsUnique();
        });
    }
}
