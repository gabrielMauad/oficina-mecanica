using MediatR;
using SharedKernel.Domain;

namespace SharedKernel.Application;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
