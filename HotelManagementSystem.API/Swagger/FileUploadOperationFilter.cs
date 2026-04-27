using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HotelManagementSystem.API.Swagger
{
    /// <summary>
    /// Fixes Swashbuckle schema generation for actions that bind a model class
    /// containing IFormFile properties (multipart/form-data file uploads).
    /// Applies to any action whose [FromForm] parameter type has at least one
    /// IFormFile property.
    /// </summary>
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // Find [FromForm] parameters whose declared type has IFormFile properties
            var fileProperties = context.MethodInfo
                .GetParameters()
                .Where(p => p.GetCustomAttributes(typeof(FromFormAttribute), true).Any())
                .SelectMany(p => p.ParameterType.GetProperties()
                    .Where(prop => prop.PropertyType == typeof(IFormFile)))
                .ToList();

            if (!fileProperties.Any()) return;

            var properties = fileProperties.ToDictionary(
                prop => prop.Name,
                _ => new OpenApiSchema { Type = "string", Format = "binary" },
                StringComparer.OrdinalIgnoreCase);

            operation.RequestBody = new OpenApiRequestBody
            {
                Required = true,
                Content  = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type       = "object",
                            Properties = properties,
                            Required   = new HashSet<string>(properties.Keys)
                        }
                    }
                }
            };
        }
    }
}
