using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace BTRoleBot
{
    public class Program
    {
        public async static Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Verbose)
                .WriteTo.File($"{Environment.GetEnvironmentVariable("logPath")}" +
                              $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss-ffff}.log", encoding: System.Text.Encoding.UTF8)
                .CreateLogger();
            
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
                .ConfigureDiscordHost((context, configuration) =>
                {
                    configuration.Token = context.Configuration["token"];
                    configuration.LogFormat = (message, exception) =>
                        message.Exception is CommandException cmdException
                            ? $"{DateTime.Now}[Command/{message.Severity}] {cmdException.Command.Aliases.First()}"
                              + $" failed to execute in {cmdException.Context.Channel}.\n" + cmdException
                            : $"[General/{message.Severity}] {message}";
                })
                .UseCommandService((context, config) =>
                {
                    config.LogLevel = LogSeverity.Verbose;
                    config.DefaultRunMode = RunMode.Sync;
                    config.CaseSensitiveCommands = false;
                });
    }
}