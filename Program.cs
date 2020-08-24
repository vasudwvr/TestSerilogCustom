using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.MSSqlServer.Sinks.MSSqlServer.Options;

/*
https://github.com/serilog/serilog-sinks-mssqlserver
https://github.com/serilog/serilog/wiki/Debugging-and-Diagnostics#selflog
https://github.com/serilog/serilog-sinks-mssqlserver
https://github.com/serilog/serilog-sinks-mssqlserver#custom-property-columns
https://github.com/serilog/serilog/wiki/Enrichment
https://stackoverflow.com/questions/39956489/writing-inside-a-specific-column-serilog-asp-net-mvc-c-sharp
https://www.ctrlaltdan.com/2018/08/14/custom-serilog-enrichers/ */

namespace TestSerilog
{
    public class ApplicationLog
    {
        public string EnvrionmentHolder => "Production";
        public ApplicationLog()
        {
        }

    }

    class EnvironmentEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory pf)
        {
            var value = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            logEvent.AddOrUpdateProperty(pf.CreateProperty("EnvrionmentHolder", value));
        }
    }
    public class Program
    {
        public static void Main(string[] args)
        {
            //var path = Directory.GetCurrentDirectory();
            //var configurationBuilder = new ConfigurationBuilder()
            //    .SetBasePath(path)
            //    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            //var configuration = configurationBuilder.Build();
            var columnOptions = new ColumnOptions
            {
                AdditionalColumns = new Collection<SqlColumn>{
                new SqlColumn
                    {
                        ColumnName = "EnvrionmentHolder", PropertyName = "EnvrionmentHolder", DataType = SqlDbType.NVarChar, DataLength = 64},
                    }
            };

            Log.Logger = new LoggerConfiguration()
                .Enrich.With<EnvironmentEnricher>()
                .Enrich.FromLogContext()
                .WriteTo.MSSqlServer(connectionString: @"Server=(localdb)\MSSQLLocalDB;Database=TestSerilog;Integrated Security=SSPI;",
        sinkOptions: new SinkOptions { TableName = "Logs", AutoCreateSqlTable = false },
        columnOptions: columnOptions)
                .CreateLogger();

            Serilog.Debugging.SelfLog.Enable(msg =>
            {
                Debug.WriteLine(msg);
                Debugger.Break();
            }
          );

            var appLog = new ApplicationLog();

            Log.Logger.Error(LogEventLevel.Error.ToString(), "{EnvrionmentHolder}", appLog.EnvrionmentHolder);

            Log.CloseAndFlush();

            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                    .UseSerilog();
                });
    }
}
