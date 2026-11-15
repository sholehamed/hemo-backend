namespace Hemo.Application.Cqrs;

/// <summary>
/// Explicit application request/handler contracts without a mediator runtime dependency.
/// HTTP is a delivery adapter; SQL Server is a persistence adapter.
/// </summary>
public interface IRequest<out TResponse>;
public interface ICommand<out TResponse> : IRequest<TResponse>;
public interface IQuery<out TResponse> : IRequest<TResponse>;
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
