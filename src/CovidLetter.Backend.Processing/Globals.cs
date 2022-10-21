using System.Runtime.CompilerServices;
using CovidLetter.Backend.Processing;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
[assembly: InternalsVisibleTo("CovidLetter.IntegrationTests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
