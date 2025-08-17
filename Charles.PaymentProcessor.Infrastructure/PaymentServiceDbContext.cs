using Charles.PaymentProcessor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PaymentSystem.Infrastructure
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<Merchant> Merchants => Set<Merchant>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.ApplyConfiguration(new MerchantConfig());
            b.ApplyConfiguration(new PaymentMethodConfig());
            b.ApplyConfiguration(new PaymentConfig());
        }
    }

    public class MerchantConfig : IEntityTypeConfiguration<Merchant>
    {
        public void Configure(EntityTypeBuilder<Merchant> e)
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ApiKeyHash).IsRequired();
            e.Property(x => x.WebhookSecret).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        }
    }

    public class PaymentMethodConfig : IEntityTypeConfiguration<PaymentMethod>
    {
        public void Configure(EntityTypeBuilder<PaymentMethod> e)
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Details).HasColumnType("jsonb");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.Merchant).WithMany(m => m.PaymentMethods).HasForeignKey(x => x.MerchantId);
        }
    }

    public class PaymentConfig : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> e)
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Reference).IsUnique();
            e.Property(x => x.Amount).HasColumnType("numeric(18,2)");
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.Metadata).HasColumnType("jsonb");
            e.Property(x => x.Status).HasConversion<int>();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasOne(x => x.Merchant).WithMany(m => m.Payments).HasForeignKey(x => x.MerchantId);
            e.HasOne(x => x.PaymentMethod).WithMany().HasForeignKey(x => x.PaymentMethodId);
        }
    }
}
