using System;
using System.Configuration;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace BTRoleBot.PreconditionAttributes
{
    public class RequireConfiguredChannelAttribute : PreconditionAttribute
    {
        private readonly ulong _channelId = ulong.Parse(new ConfigurationBuilder().AddJsonFile("appsettings.Production.json").Build()["channel"]);
        
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (!(context.User is IGuildUser guildUser))
                return Task.FromResult(PreconditionResult.FromError("This command cannot be executed outside of a guild."));

            return Task.FromResult(context.Channel.Id != _channelId 
                ? PreconditionResult.FromError($"{context.User.Mention} Не тот канал! Ты зачем контору палишь? У тебя вообще своя роль есть? Кто тебе про эту команду рассказал?") 
                : PreconditionResult.FromSuccess());
        }
    }
}