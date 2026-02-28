using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ReceptionistAgent.Api.Swagger;

/// <summary>
/// Agrega el header X-Tenant-Id como parámetro requerido
/// en todos los endpoints de Swagger UI.
/// </summary>
public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Tenant-Id",
            In = ParameterLocation.Header,
            Required = true,
            Description = "ID del tenant (ej: clinica-vista-clara, salon-bella)",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = System.Text.Json.Nodes.JsonValue.Create("clinica-vista-clara")
            }
        });
    }
}
