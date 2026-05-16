using SharedKernel.Domain;

namespace SharedKernel.Application;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<IDomainEvent> CollectDomainEvents();
    void ClearDomainEvents();
}
