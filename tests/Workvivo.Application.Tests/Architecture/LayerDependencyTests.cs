using NetArchTest.Rules;
using Shouldly;
using Workvivo.Application.Bases;
using Workvivo.Domain.Abstractions.Interfaces;
using Xunit;

namespace Workvivo.Application.Tests.Architecture;

/// <summary>
/// The solution's project references run Application -> Infrastructure, which is the
/// reverse of textbook Clean Architecture. That shape is the template's and is not
/// being changed, so the layering discipline cannot be enforced by the compiler.
///
/// These tests enforce it instead: application code may depend on abstractions, never
/// on concrete infrastructure. Without them the inverted reference is an open door
/// that the first person in a hurry walks through.
/// </summary>
public class LayerDependencyTests
{
    private static System.Reflection.Assembly ApplicationAssembly => typeof(MappingProfileBase).Assembly;

    private static System.Reflection.Assembly DomainAssembly => typeof(IUnitOfWork).Assembly;

    [Fact]
    public void Application_does_not_reference_the_EF_DbContext_directly()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Workvivo.Infrastructure.DBContext")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Application code must go through IUnitOfWork, not the DbContext: "
            + Describe(result));
    }

    [Fact]
    public void Application_does_not_reference_concrete_repositories()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Workvivo.Infrastructure.Repositories")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Application code must depend on IBaseRepository / IUnitOfWork: " + Describe(result));
    }

    [Fact]
    public void Application_does_not_reference_concrete_storage_or_caching_providers()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Workvivo.Infrastructure.Storage",
                "Workvivo.Infrastructure.Caching",
                "Workvivo.Infrastructure.Jobs")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(
            "Swapping local disk for blob storage, or memory for Redis, must not touch "
            + "application code: " + Describe(result));
    }

    [Fact]
    public void Domain_depends_on_no_other_Workvivo_project()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Workvivo.Application",
                "Workvivo.Infrastructure",
                "Workvivo.API")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue("Domain must stay dependency-free: " + Describe(result));
    }

    [Fact]
    public void Every_pipeline_behavior_is_sealed()
    {
        // Behaviours run for every request in the system. Leaving one open to
        // inheritance invites a subclass that quietly changes the pipeline's semantics.
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespace("Workvivo.Application.Behaviors")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        result.FailingTypeNames is null
            ? "no offending types reported"
            : string.Join(", ", result.FailingTypeNames);
}
