using ahis.template.domain.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ahis.template.application.Interfaces.Repositories
{
    /// <summary>
    /// Generic repository contract for data access operations.
    /// Designed for Clean Architecture and EF Core.
    /// </summary>
    /// <typeparam name="T">Entity type deriving from BaseEntity</typeparam>
    public interface IGenericGuidRepository<T> where T : BaseGuidEntity
    {
        #region Query


        /// <summary>
        /// Get all non-deleted entities.
        /// </summary>
        Task<IReadOnlyList<T>> GetAllAsync(bool asNoTracking = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get an entity by its primary key.
        /// </summary>
        /// <param name="id">Primary key value.</param>
        /// <param name="asNoTracking">If true the returned entity will be detached (no tracking).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The entity or null if not found or soft-deleted.</returns>
        Task<T?> GetByIdAsync(object id, bool asNoTracking = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Query entities by predicate (automatically filters out soft-deleted entities).
        /// </summary>
        Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the first entity matching predicate or null.
        /// </summary>
        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check whether an entity exists matching the predicate.
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Count entities matching optional predicate (excludes soft-deleted entities by default).
        /// </summary>
        Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        IQueryable<T> GetQueryable();

        #endregion

        #region Command

        /// <summary>
        /// Add a new entity to the context. Caller is responsible for calling SaveChangesAsync on the repository/unit-of-work.
        /// </summary>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple entities to the context.
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Mark entity as modified in the context.
        /// </summary>
        void Update(T entity);

        /// <summary>
        /// Mark multiple entities as modified in the context.
        /// </summary>
        void UpdateRange(IEnumerable<T> entities);

        /// <summary>
        /// Remove an entity from the context (physical delete).
        /// </summary>
        void Remove(T entity);

        /// <summary>
        /// Remove multiple entities from the context (physical delete).
        /// </summary>
        void RemoveRange(IEnumerable<T> entities);

        /// <summary>
        /// Soft-delete an entity (sets IsDelete flag and marks entity updated).
        /// </summary>
        void SoftDelete(T entity);

        /// <summary>
        /// Restore a soft-deleted entity.
        /// </summary>
        void Restore(T entity);

        #endregion

        //#region Save & Transaction

        ///// <summary>
        ///// Persist changes to the database.
        ///// </summary>
        //Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        ///// <summary>
        ///// Begin a database transaction.
        ///// </summary>
        //Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        ///// <summary>
        ///// Commit the current database transaction.
        ///// </summary>
        //Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        ///// <summary>
        ///// Rollback the current database transaction.
        ///// </summary>
        //Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

        //#endregion
    }
}
