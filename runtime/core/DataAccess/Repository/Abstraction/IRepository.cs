using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CivicCommunicator.DataAccess.Repository.Abstraction
{
    public interface IRepository<TEntity> where TEntity : class
    {
        IEnumerable<TEntity> GetAll();

        IQueryable<TEntity> AsQueryable();

        TEntity Get(object id);
        void Add(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
    }
}
