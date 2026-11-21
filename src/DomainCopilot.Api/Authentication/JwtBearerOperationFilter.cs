using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DomainCopilot.Api.Authentication;

public sealed class JwtBearerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isAnonymous = context.MethodInfo.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any()
            || context.MethodInfo.DeclaringType?.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true).Any() == true;

        if (isAnonymous)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", context.Document)] = []
        });
    }
}
