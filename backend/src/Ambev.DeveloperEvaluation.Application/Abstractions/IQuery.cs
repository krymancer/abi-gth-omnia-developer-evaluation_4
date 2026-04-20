using MediatR;

namespace Ambev.DeveloperEvaluation.Application.Abstractions;

#pragma warning disable S3246 // IRequest<T> is not covariant by design
public interface IQuery<TResponse> : IRequest<TResponse> { }
#pragma warning restore S3246
