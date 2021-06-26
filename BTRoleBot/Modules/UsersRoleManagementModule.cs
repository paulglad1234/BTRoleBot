using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using BTRoleBot.PreconditionAttributes;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Database;

namespace BTRoleBot.Modules
{
    [Group("myrole")]
    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    [RequireConfiguredChannel]
    public class UsersRoleManagementModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<UsersRoleManagementModule> _logger;
        private readonly IServiceProvider _serviceProvider;

        public UsersRoleManagementModule(ILogger<UsersRoleManagementModule> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        private async Task<IRole> GetRoleAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<UserRoleDbContext>();
            var ur = await context.GetByUserId(Context.User.Id);
            
            if (ur != null)
            {
                var role = Context.Guild.GetRole(ur.RoleId);
                if (role != null)
                    return role;
                await context.RemoveByUserId(ur.UserId);
            }
            
            await ReplyAsync($"{Context.User.Mention} у тебя нет своей роли, ты чё меня дёргаешь?");
            return null;
        }

        [Command("name")]
        [Summary("Renames the user`s own role.")]
        public async Task Rename(
            [Summary("New name of the role")]
            [Remainder]
            string name)
        {
            var role = await GetRoleAsync();
            if (role == null) return;

            await role.ModifyAsync(properties => properties.Name = name);

            _logger.LogInformation($"Role {role} was renamed.");
            await ReplyAsync($"Роль {role.Mention} переименована.");
        }

        [Command("color")]
        [Summary("Changes the color of the user`s own role")]
        public async Task Recolor([Summary("New color of the role")] Color color)
        {
            var role = await GetRoleAsync();
            if (role == null) return;

            await role.ModifyAsync(properties => properties.Color = color);
            
            _logger.LogInformation($"Role {role} changed color.");
            await ReplyAsync($"Роль {role.Mention} покрашена!");
        }
    }
}