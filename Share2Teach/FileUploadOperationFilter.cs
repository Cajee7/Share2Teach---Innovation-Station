using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content.ContainsKey("multipart/form-data") == true)
        {
            var fileParams = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile))
                .ToList();

            foreach (var fileParam in fileParams)
            {
                var parameter = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    }
                };

                if (operation.RequestBody.Content.ContainsKey("multipart/form-data"))
                {
                    operation.RequestBody.Content["multipart/form-data"].Schema.Properties[fileParam.Name] = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "binary"
                    };
                }
            }
        }
    }
}
