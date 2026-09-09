using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Workvivo.API.Filters;

/// <summary>
/// Pre-fills the Swagger "Try it out" body for the login endpoint with the
/// development seed account, so a new developer can call the API immediately.
/// Swagger UI is only mapped outside production, so this never ships publicly.
/// </summary>
public class ExampleSchemaFilter : ISchemaFilter
{
    // Microsoft.OpenApi v2 (Swashbuckle 10) hands filters the IOpenApiSchema
    // interface and models examples as System.Text.Json nodes rather than the
    // old OpenApiAny wrappers.
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(LoginModel) || schema is not OpenApiSchema editable)
        {
            return;
        }

        editable.Example = new JsonObject
        {
            ["userName"] = "dev",
            ["password"] = "P@55w0rd",
        };
    }
}
