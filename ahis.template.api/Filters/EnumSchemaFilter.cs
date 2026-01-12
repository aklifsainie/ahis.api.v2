using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace ahis.template.api.Filters
{


    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (!context.Type.IsEnum)
                return;

            schema.Description ??= "Allowed values:";

            foreach (var name in Enum.GetNames(context.Type))
            {
                var member = context.Type.GetMember(name).First();
                var summary = member
                    .GetCustomAttribute<EndpointSummaryAttribute>();
                    //.Description;

                var value = Convert.ToInt32(Enum.Parse(context.Type, name));

                schema.Description += $"\n- `{value}` = {name}";
            }
        }
    }

    //public class EnumSchemaFilter : ISchemaFilter
    //{
    //    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    //    {
    //        if (!context.Type.IsEnum)
    //            return;

    //        schema.Description ??= "Allowed values:";

    //        foreach (var enumName in Enum.GetNames(context.Type))
    //        {
    //            var enumValue = Convert.ToInt32(Enum.Parse(context.Type, enumName));
    //            schema.Description += $"\n- `{enumValue}` = {enumName}";
    //        }
    //    }
    //}

}
