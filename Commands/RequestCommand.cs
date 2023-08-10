using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.Options;

namespace AutoRequestStore.Commands
{
    [Command]
    public class RequestCommand : ICommand
    {
        public RequestCommand(IOptions<ConnectionSettings> connection)
        {
            Console.Out.WriteLine(connection.Value.Endpoint);
        }

        //[CommandParameter(0, Description = "Value whose logarithm is to be found.")]
        //public double Millisecons { get; init; }

        // Name: --interval
        // Short name: -i
        [CommandOption("interval", 'i', Description = "Interval in milliseconds.")]
        public double Interval { get; init; } = 1000;


        public async ValueTask ExecuteAsync(IConsole console)
        {

        }
    }
}
