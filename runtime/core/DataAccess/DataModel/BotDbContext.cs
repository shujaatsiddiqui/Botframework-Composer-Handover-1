using CivicCommunicator.DataAccess.DataModel.Models;
using Microsoft.EntityFrameworkCore;

namespace CivicCommunicator.DataAccess.DataModel
{
    public class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions options) : base(options) {}

        public DbSet<User> Users { get; set; }

        public DbSet<BotReply> BotReplies { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<ConversationRequest> Requests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(x => x.UserId);
            modelBuilder.Entity<User>().HasIndex(x => x.ChatId).IsUnique();
            modelBuilder.Entity<User>().Property(x => x.HandlingDomain);

            modelBuilder.Entity<Message>().HasKey(x => x.MessageId);
            modelBuilder.Entity<Message>().HasOne(x => x.From).WithMany(x => x.Messages).HasForeignKey(x => x.FromId).OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<BotReply>().HasKey(x => x.BotReplyId);
            modelBuilder.Entity<BotReply>().HasOne(x => x.To).WithMany(x => x.BotReplies).HasForeignKey(x => x.ToId).OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<ConversationRequest>().HasKey(x => x.ConversationRequestId);
            modelBuilder.Entity<ConversationRequest>().HasOne(x => x.Requester).WithMany(x => x.Requested).HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.ClientSetNull);
            modelBuilder.Entity<ConversationRequest>().HasOne(x => x.Agent).WithMany(x => x.Handled).HasForeignKey(x => x.AgentId).OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<ConversationRequest>()
                .Property(c => c.State)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);

        }
    }
}
