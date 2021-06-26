using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BTRoleBot.TypeReaders;
using Discord;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BTRoleBot
{
    public class CommandHandler: InitializedService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CommandHandler> _logger;
        
        private readonly string _prefix;
        //private readonly ulong _channelId = ulong.Parse(ConfigurationManager.AppSettings["channel"]);

        // Retrieve client and CommandService instance via ctor
        public CommandHandler(DiscordSocketClient client, CommandService commandService, 
            IServiceProvider serviceProvider, ILogger<CommandHandler> logger, IConfiguration configuration)
        {
            _commandService = commandService;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _client = client;
            _prefix = configuration["prefix"];
        }

        public override async Task InitializeAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();

            _client.MessageReceived += HandleMessageAsync;
            _commandService.CommandExecuted += CommandExecutedAsync;
            _commandService.AddTypeReader(typeof(Color), new ColorTypeReader());
            await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), scope.ServiceProvider);
        }

        private async Task HandleMessageAsync(SocketMessage incomingMessage)
        {
            // Don't process the command if it was a system message
            if (!(incomingMessage is SocketUserMessage {Source: MessageSource.User} message)) return;
            //if (message.Channel.Id != _channelId) return;

            if (string.IsNullOrEmpty(_prefix))
            {
                _logger.LogError("Listening prefix is empty");
                return;
            }

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;
            
            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasStringPrefix(_prefix, ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(_client, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await _commandService.ExecuteAsync(context, argPos, _serviceProvider);
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified || result.IsSuccess)
                return;
            
            await context.Channel.SendMessageAsync($"{result.ErrorReason}");
        }
    }
}
