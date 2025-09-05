using Microsoft.OpenApi.Models;
using Quipu.ParameterizationExtractor.WebApi.Services;
using Quipu.ParameterizationExtractor.WebApi.Models;
using Quipu.ParameterizationExtractor.Logic.Interfaces;
using Quipu.ParameterizationExtractor.Logic.MSSQL;
using Quipu.ParameterizationExtractor.DSL.Connector;
using Quipu.ParameterizationExtractor;
using Quipu.ParameterizationExtractor.Common;
using Quipu.ParameterizationExtractor.Configs;
using ParameterizationExtractor.Logic.MSSQL;
using Quipu.ParameterizationExtractor.Logic.Configs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "ParameterizationExtractor API", 
        Version = "v1",
        Description = "REST API for database extraction and SQL script generation"
    });
});

builder.Services.AddScoped<IExtractionService, ExtractionService>();
builder.Services.AddScoped<IDSLConnector, FparsecConnector>();
builder.Services.AddScoped<PackageProcessor>();

builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ICanSerializeConfigs, ConfigSerializer>();
builder.Services.AddTransient<ISqlBuilder, MSSqlBuilder>();
builder.Services.AddScoped<ISourceSchema, MSSQLSourceSchema>();
builder.Services.AddScoped<IObjectMetaDataProvider, ObjectMetaDataProvider>();
builder.Services.AddScoped<IUnitOfWorkFactory, UnitOfWorkFactory>();
builder.Services.AddTransient<IDependencyBuilder, DependencyBuilder>();
builder.Services.AddTransient<IMetaDataInitializer, MetaDataInitializer>();
builder.Services.AddScoped<IConnectionStringResolver, ConnectionStringResolver>();

builder.Services.AddScoped<IAppArgs>(provider => new ApiAppArgs());

builder.Services.AddSingleton<IExtractConfiguration>(provider =>
{
    var dslConnector = provider.GetRequiredService<IDSLConnector>();
    var configSerializer = new ConfigSerializer(dslConnector);
    try
    {
        return configSerializer.GetGlobalConfig();
    }
    catch
    {
        return new GlobalExtractConfiguration();
    }
});

builder.Services.AddLogging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
