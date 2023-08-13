using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoRequestStore;
using CliFx;
using AutoRequestStore.Commands;

var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

var services = new ServiceCollection();
services.AddTransient<RequestCommand>();
services.AddOptions<ConnectionSettings>().Bind(configuration.GetSection(ConnectionSettings.Section));

var serviceProvider = services.BuildServiceProvider();

return await new CliApplicationBuilder()
        .AddCommandsFromThisAssembly()
        .UseTypeActivator(serviceProvider.GetRequiredService)
        .Build()
        .RunAsync();