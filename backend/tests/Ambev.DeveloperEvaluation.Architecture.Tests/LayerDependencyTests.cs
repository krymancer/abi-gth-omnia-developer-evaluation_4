using Ambev.DeveloperEvaluation.Application.Abstractions;
using Ambev.DeveloperEvaluation.Domain.SharedKernel;
using Ambev.DeveloperEvaluation.Infrastructure.Persistence;
using FluentAssertions;
using NetArchTest.Rules;

namespace Ambev.DeveloperEvaluation.Architecture.Tests;

public sealed class LayerDependencyTests
{
    private static readonly string ApplicationAssembly = typeof(ICommand).Assembly.FullName!;
    private static readonly string InfrastructureAssembly = typeof(AppDbContext).Assembly.FullName!;
    private static readonly string ApiAssembly = typeof(Program).Assembly.FullName!;

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        var result = Types.InAssembly(typeof(AggregateRoot<>).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApplicationAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain must not reference Application. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(AggregateRoot<>).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Domain must not reference Infrastructure. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]

    public void Application_ShouldNotDependOn_Infrastructure()
    {
        var result = Types.InAssembly(typeof(ICommand).Assembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application must not reference Infrastructure. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Application_ShouldNotDependOn_Api()
    {
        var result = Types.InAssembly(typeof(ICommand).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiAssembly)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"Application must not reference Api. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Handlers_ShouldResideIn_ApplicationNamespace()
    {
        var result = Types.InAssembly(typeof(ICommand).Assembly)
            .That()
            .HaveNameEndingWith("Handler")
            .Should()
            .ResideInNamespace("Ambev.DeveloperEvaluation.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All handlers must reside in Application namespace. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }

    [Fact]
    public void Commands_ShouldResideIn_ApplicationNamespace()
    {
        var result = Types.InAssembly(typeof(ICommand).Assembly)
            .That()
            .ImplementInterface(typeof(ICommand))
            .Or()
            .ImplementInterface(typeof(ICommand<>))
            .Should()
            .ResideInNamespace("Ambev.DeveloperEvaluation.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(
            $"All ICommand implementations must reside in Application namespace. Violations: {string.Join(", ", result.FailingTypeNames ?? [])}");
    }
}
