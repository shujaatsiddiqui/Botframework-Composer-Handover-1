using Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFramework.Composer.DAL.DataAccess.DataModel
{
    public class BotDbContext : DbContext
    {
        private readonly string connectionString;

        public BotDbContext()
        {
            this.connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer(@"Server=tcp:chatbot-server-demo.database.windows.net,1433;Initial Catalog=chatbotdemo;Persist Security Info=False;User ID=serveradmin;Password=c1v1c@admin;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
            }
        }
        public BotDbContext(DbContextOptions options) : base(options) { }

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
