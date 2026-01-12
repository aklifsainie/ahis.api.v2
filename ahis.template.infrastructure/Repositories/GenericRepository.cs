using ahis.template.application.Interfaces.Repositories;
using ahis.template.domain.Models.Entities;
using ahis.template.infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ahis.template.infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        private IDbContextTransaction? _currentTransaction;

        public GenericRepository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        #region Query

        // Backward-compatible parameterless overload
        public async Task<List<T>> GetAllAsync()
        {
            return (await GetAllAsync(true, CancellationToken.None)).ToList();
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.Where(x => !x.IsDelete);
            if (asNoTracking) query = query.AsNoTracking();
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<T?> GetByIdAsync(object id, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new[] { id }, cancellationToken);
            if (entity == null || entity.IsDelete) return null;

            if (asNoTracking)
                _context.Entry(entity).State = EntityState.Detached;

            return entity;
        }

        public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.Where(predicate).Where(x => !x.IsDelete);
            if (asNoTracking) query = query.AsNoTracking();
            return await query.ToListAsync(cancellationToken);
        }

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.Where(predicate).Where(x => !x.IsDelete);
            if (asNoTracking) query = query.AsNoTracking();
            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(predicate, cancellationToken);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            IQueryable<T> query = _dbSet.Where(x => !x.IsDelete);
            if (predicate != null) query = query.Where(predicate);
            return await query.CountAsync(cancellationToken);
        }

        #endregion

        #region Command

        public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddAsync(entity, cancellationToken);
            return entity;
        }

        public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            await _dbSet.AddRangeAsync(entities, cancellationToken);
        }

        public void Update(T entity) => _dbSet.Update(entity);

        public void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

        public void Remove(T entity) => _dbSet.Remove(entity);

        public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

        public void SoftDelete(T entity)
        {
            entity.IsDelete = true;
            Update(entity);
        }

        public void Restore(T entity)
        {
            entity.IsDelete = false;
            Update(entity);
        }

        #endregion

        //#region Save & Transaction (to satisfy application-level IGenericRepository)

        //public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        //    => await _context.SaveChangesAsync(cancellationToken);

        //public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        //{
        //    if (_currentTransaction != null)
        //        return;

        //    _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        //}

        //public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        //{
        //    if (_currentTransaction == null)
        //        return;

        //    await _currentTransaction.CommitAsync(cancellationToken);
        //    await _currentTransaction.DisposeAsync();
        //    _currentTransaction = null;
        //}

        //public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        //{
        //    if (_currentTransaction == null)
        //        return;

        //    await _currentTransaction.RollbackAsync(cancellationToken);
        //    await _currentTransaction.DisposeAsync();
        //    _currentTransaction = null;
        //}

        //#endregion
    }
}
