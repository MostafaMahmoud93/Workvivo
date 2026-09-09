using Microsoft.OpenApi;
using Workvivo.API.Filters;

namespace Workvivo.API.Extensions;

public static class ConfigureSwagger
{
    public static IServiceCollection AddWorkvivoSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Workvivo API",
                Version = "v1",
                Description =
                    "Employee experience and internal communication platform. "
                    + "All endpoints require a bearer token unless marked otherwise.",
            });

            // Lets "Authorize" in the UI hold a token for the whole session, so a
            // developer pastes it once instead of on every request.
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste the access token only - Swagger adds the \"Bearer \" prefix.",
            });

            // Swashbuckle 10 takes a factory so the requirement can resolve the scheme
            // against the document being generated.
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
            });

            options.EnableAnnotations();
            options.SchemaFilter<ExampleSchemaFilter>();

            // Two types in different feature folders can legitimately share a short
            // name (PostDto, CommentDto). Without this the generator throws on the
            // collision instead of disambiguating.
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
        });

        return services;
    }
}
