using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace BTRoleBot.PreconditionAttributes
{
    public class RequireAdminRoleAttribute : PreconditionAttribute
    {
        private readonly ulong[] _roleIds;

        public RequireAdminRoleAttribute()
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.Production.json").Build();
            _roleIds = config["adminRoleIds"].Split(',')
                .Select(ulong.Parse).ToArray();
        }

        public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context,
            CommandInfo command, IServiceProvider services)
        {
            if (!(context.User is IGuildUser guildUser))
                return PreconditionResult.FromError("This command cannot be executed outside of a guild.");

            var guild = guildUser.Guild;
            if (guild.Roles.All(r => _roleIds.Contains(r.Id)))
                return PreconditionResult.FromError(
                    $"The guild does not have the role ({_roleIds.ToArray()}) required to access this command.");

            return guildUser.RoleIds.Any(rId => _roleIds.Contains(rId)) || 
                   (await guild.GetOwnerAsync()).Id == guildUser.Id
                ? PreconditionResult.FromSuccess()
                : PreconditionResult.FromError("У тебя нет на это права!");
        }
    }
}
