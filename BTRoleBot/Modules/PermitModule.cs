using System;
using System.Configuration;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using BTRoleBot.PreconditionAttributes;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Database;

namespace BTRoleBot.Modules
{
    [Group("role")]
    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    [RequireAdminRole]
    public class PermitModule : ModuleBase<SocketCommandContext>
    {
        private readonly ILogger<PermitModule> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly ulong _channelId;

        public PermitModule(ILogger<PermitModule> logger, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _channelId = ulong.Parse(configuration["channel"]);
        }

        [Command("give", RunMode = RunMode.Async)]
        [Summary("Create user`s own role and give a permission to manage it.")]
        public async Task Permit(
            [Summary("The user to give the permission to.")]
            IGuildUser guildUser)
        {
            _logger.LogInformation($"User {Context.User} gives permission to {guildUser}");
            
            var guild = guildUser.Guild;
            IRole role;
            
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<UserRoleDbContext>();
            // Check if the user already has a role
            var ur = await context.GetByUserId(guildUser.Id);
            if (ur != null)
            {
                await ReplyAsync($"У {guildUser.Mention} уже есть своя роль {guild.GetRole(ur.RoleId).Mention}. Зачем ему ещё одна?");
                return;
            }
            
            // Create the role if not and give it to the user
            var roleName = string.IsNullOrEmpty(guildUser.Nickname) ? guildUser.Username : guildUser.Nickname;
            role = await guild.CreateRoleAsync(
                $"{roleName} role", color: Color.Green, isHoisted: false, isMentionable: true);
            await guildUser.AddRoleAsync(role);

            // Save in db
            await context.Add(guildUser.Id, role.Id);
            
            // To show the nickname in the color of the role, reorder the role to be on top
            var position = await CalculateRolePosition();
            
            // todo think this out, its terrible. Better find a way to get bot`s guild position or just leave the highest possible position
            /*var ok = false;
            while (!ok)
            {
                try
                {
                    await guild.ReorderRolesAsync(new[] {new ReorderRoleProperties(role.Id, position)});
                    ok = true;
                }
                catch (Exception)
                {
                    position--;
                }
            }*/
            
            // Reorder roles so the color of the user`s own role is used
            await guild.ReorderRolesAsync(new[] {new ReorderRoleProperties(role.Id, position)});
            
            // The commands to manage the role are gonna be called in this channel. The channel is hidden by default.
            // So give the permission to the role. The permission will be removed when the role is deleted.
            var manageRoleChannel = await guild.GetChannelAsync(_channelId);
            await manageRoleChannel.AddPermissionOverwriteAsync(role,
                new OverwritePermissions(viewChannel: PermValue.Allow));
            
            await ReplyAsync($"{guildUser.Mention} получил свою роль!");
            await guildUser.SendMessageAsync($"Поздравляю с получением роли на сервере `{guild.Name}`!\n" +
                                             $"Тебе открылся специальный канал `{manageRoleChannel}`, управление своей ролью происходит только в нём!\n\n" +
                                             "Для управления своей ролью используется команда `!myrole`\n\n" +
                                             "Пиши `!myrole name <желаемое название роли>`, чтобы изменить название своей роли.\n\n" +
                                             "`!myrole color #<шестизначный HEX код цвета>` используется, чтобы изменить цвет роли. Например `!myrole color #ffffff` поменяет цвет на белый.\n\n" +
                                             "Подсмотреть HEX код цвета можно, например, здесь: https://sdelatlending.ru/generator-cveta-html\n" +
                                             "Там есть ползунки, где ты можешь выбрать понравившийся цвет, и тебе скажут код. (Бери только 6 знаков из второй ячейки, мне нужны только они!)\n\n" +
                                             "Если вдруг я не реагирую на сообщения и не в сети, в закрепе того канала лежит ссылка. Нужно открыть её и дождаться загрузки.\n" +
                                             "Сраные кожаные мешки, которые создали хостинг, на который меня залил другой кожаный мешок (Получается, что Симен - мошонка KEKW), " +
                                             "требуют, чтобы кто-то заходил на \"сайт\", чтобы он был включен. Такие дела Sadge\n\n");
        }

        private Task<int> CalculateRolePosition() => Task.FromResult(Context.Guild.Roles.Max(role => role.Position) - 1);
        
        [Command("remove")]
        [Summary("Deletes user`s own role")]
        public async Task Deny(
            [Summary("The user to remove the permission from.")]
            IGuildUser guildUser)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<UserRoleDbContext>();
            // Check if the user`s role exists
            var ur = await context.GetByUserId(guildUser.Id);
            if (ur == null)
            {
                await ReplyAsync($"У {guildUser.Mention} и так нет своей роли. Ты хочешь забрать у него вообще всё?");
                return;
            }

            // Delete the role
            await guildUser.Guild.GetRole(ur.RoleId).DeleteAsync();

            // Remove it from db
            await context.RemoveByUserId(guildUser.Id);
            
            _logger.LogInformation($"User {Context.User} removes permission from {guildUser}");
            
            await ReplyAsync($"Окей, я забрал роль у {guildUser.Mention}.");
        }

        [Command("remove")]
        [Summary("Another way to delete user`s own role")]
        public async Task Deny(
            [Summary("The role to delete")] IRole role)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetService<UserRoleDbContext>();
            // Check if the role exists and was given by the bot
            var ur = await context.GetByRoleId(role.Id);
            if (ur == null)
            {
                await ReplyAsync(Context.Guild.Roles.Any(socketRole => socketRole.Id == role.Id)
                    ? "Эту роль создавал не я, так что и удалять не мне!"
                    : "Да такой роли вообще нет на сервере!");
                return;
            }

            // Remove from db
            await context.RemoveByRoleId(role.Id);
            
            _logger.LogInformation($"User {Context.User} deletes role {role}");

            // Delete the role
            await role.DeleteAsync();

            await ReplyAsync($"Окей, роль удалена.");
        }
    }
}
