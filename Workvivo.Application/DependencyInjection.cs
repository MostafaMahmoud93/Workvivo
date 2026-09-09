using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Workvivo.Application.Behaviors;
using Workvivo.Application.Bases;

namespace Workvivo.Application;

/// <summary>
/// Composition root for the use-case layer: MediatR, its pipeline, the validators and
/// the mapping profiles.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Registration order is execution order, outermost first, and it matters:
            //
            //   logging      wraps everything, so a rejected request is still recorded
            //   performance  measures the real cost, validation included
            //   validation   runs before any work, so a bad request never opens a
            //                transaction or warms a cache entry
            //   caching      sits inside validation but outside the handler
            //   transaction  innermost, around the handler alone, held for as short a
            //                time as possible
            config.AddOpenBehavior(typeof(RequestLoggingBehavior<,>));
            config.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
            config.AddOpenBehavior(typeof(CachingBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Registers every Profile in this assembly (MapperProfile/* and the per-feature
        // Mappings folders). Every service here injects IMapper, so without this the
        // container cannot construct a single one of them.
        services.AddAutoMapper(_ => { }, typeof(MappingProfileBase).Assembly);

        return services;
    }
}
