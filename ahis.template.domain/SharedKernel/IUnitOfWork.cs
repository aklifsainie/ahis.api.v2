using System.Threading;
using System.Threading.Tasks;

namespace ahis.template.domain.SharedKernel
{
    /// <summary>
    /// Unit of Work abstraction for coordinating Save and Transaction operations across repositories.
    /// </summary>
    public interface IUnitOfWork
    {
        /// <summary>
        /// Persist changes to the underlying store.
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begin a database transaction.
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commit the active transaction.
        /// </summary>
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rollback the active transaction.
        /// </summary>
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
