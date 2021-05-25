using CivicCommunicator.DataAccess.DataModel;
using CivicCommunicator.DataAccess.Repository.Abstraction;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CivicCommunicator.DataAccess.Repository.Implementation
{
    public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
    {
        private readonly BotDbContext context;
        private readonly DbSet<TEntity> table;

        public Repository(BotDbContext context)
        {
            this.context = context;
            this.table = this.context.Set<TEntity>();
        }

        public void Add(TEntity entity)
        {
            this.table.Add(entity);
            this.context.SaveChanges();
        }

        public IQueryable<TEntity> AsQueryable() => this.table.AsQueryable<TEntity>();

        public void Delete(TEntity entity)
        {
            this.table.Remove(entity);
            this.context.SaveChanges();
        }

        public TEntity Get(object id) => this.table.Find(id);

        public IEnumerable<TEntity> GetAll() => this.table.ToList();

        public void Update(TEntity entity)
        {
            this.table.Attach(entity);
            this.context.Entry(entity).State = EntityState.Modified;
            this.context.SaveChanges();
        }
    }
}
