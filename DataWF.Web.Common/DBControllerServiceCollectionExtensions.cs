﻿using DataWF.Data;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Microsoft.AspNetCore.JsonPatch;

namespace DataWF.Web.Common
{
    public static class DBControllerServiceCollectionExtensions
    {
        public static Assembly AddDBController(this IServiceCollection services, DBSchema schema)
        {
            return services
                .AddMvcCore()
                .AddApiExplorer()
                .AddJsonFormatters()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Converters.Add(new DBItemJsonConverter());
                })
                .AddDBController(schema);
        }

        public static Assembly AddDBController(this IMvcCoreBuilder builder, DBSchema schema)
        {
            var generator = new DBControllerGenerator();
            var controllersAssembly = generator.Generate(schema);
            builder.AddApplicationPart(controllersAssembly);
            return controllersAssembly;
        }

        public static Assembly AddDBController(this IMvcBuilder builder, DBSchema schema)
        {
            var generator = new DBControllerGenerator();
            var controllersAssembly = generator.Generate(schema);
            builder.AddApplicationPart(controllersAssembly);
            return controllersAssembly;
        }
    }
}