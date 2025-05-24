using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.Helpers;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.MethodInfo.GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile) || p.ParameterType == typeof(IFormCollection));

        foreach (var parameter in fileParameters)
        {
            var content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["incident"] = new OpenApiSchema
                            {
                                Type = "object",
                                Properties = new Dictionary<string, OpenApiSchema>
                                {
                                    ["title"] = new OpenApiSchema { Type = "string", Description = "Title of the incident" },
                                    ["description"] = new OpenApiSchema { Type = "string", Description = "Description of the incident" },
                                    ["latitude"] = new OpenApiSchema { Type = "number", Format = "double", Description = "Latitude coordinate" },
                                    ["longitude"] = new OpenApiSchema { Type = "number", Format = "double", Description = "Longitude coordinate" },
                                    ["address"] = new OpenApiSchema { Type = "string", Description = "Address of the incident" },
                                    ["zipCode"] = new OpenApiSchema { Type = "string", Description = "Zip code" },
                                    ["priority"] = new OpenApiSchema { Type = "integer", Description = "Priority level (0-3)" }
                                },
                                Required = new HashSet<string> { "title", "description" }
                            },
                            ["photos"] = new OpenApiSchema
                            {
                                Type = "array",
                                Items = new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary"
                                },
                                Description = "Photo files to upload"
                            }
                        }
                    }
                }
            };

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = content
            };
        }
    }
} 