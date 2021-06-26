using System;
using System.Globalization;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace BTRoleBot.TypeReaders
{
    public class ColorTypeReader: TypeReader
    {
        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input,
            IServiceProvider services)
        {
            if (input.Length == 7 && input[0] == '#' 
                                  && int.TryParse(input.Substring(1, 2), 
                                      NumberStyles.HexNumber, null, out var r) 
                                  && int.TryParse(input.Substring(3, 2), 
                                      NumberStyles.HexNumber, null, out var g) 
                                  && int.TryParse(input.Substring(5, 2), 
                                      NumberStyles.HexNumber, null, out var b))
                return Task.FromResult(TypeReaderResult.FromSuccess(new Color(r, g, b)));

            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed,
                $"{context.User.Mention} Это что за цвет такой, я не понял?\n"));
        }
    }
}