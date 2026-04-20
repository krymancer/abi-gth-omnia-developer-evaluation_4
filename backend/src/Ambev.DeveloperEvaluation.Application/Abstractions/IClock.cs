namespace Ambev.DeveloperEvaluation.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
