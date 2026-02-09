using Microsoft.Extensions.Logging;
using System.Text;

namespace AppSwitcher.CLI;

internal class CliHandler(
    ILogger<CliHandler> logger,
    IServiceProvider serviceProvider,
    CliBuilder builder,
    CliOptions options)
{
    public bool Handle(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }

        var matchedCommands = new List<CliBuilder.CommandRegistration>();
        var i = 0;

        while (i < args.Length)
        {
            var arg = args[i];
            logger.LogDebug("Processing argument: {Arg}", arg);

            if (arg == "--help")
            {
                ShowHelp();
                return true;
            }

            var flagOption = builder.FlagOptions.FirstOrDefault(o => o.Name == arg);
            if (flagOption != null)
            {
                logger.LogDebug("Matched flag option: {Name}", flagOption.Name);
                flagOption.Handler(options);
                i++;
                continue;
            }

            var valuedOption = builder.ValuedOptions.FirstOrDefault(o => o.Name == arg);
            if (valuedOption != null)
            {
                if (i + 1 >= args.Length)
                {
                    MessageBox.Show($"Option {arg} requires a value.", "AppSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return true;
                }

                var value = args[i + 1];
                logger.LogDebug("Matched valued option: {Name} = {Value}", valuedOption.Name, value);
                valuedOption.Handler(options, value);
                i += 2;
                continue;
            }

            var command = builder.Commands.FirstOrDefault(c => c.Name == arg);
            if (command != null)
            {
                logger.LogDebug("Matched command: {Name}", command.Name);
                matchedCommands.Add(command);
                i++;
                continue;
            }

            logger.LogWarning("Unknown argument: {Arg}", arg);
            i++;
        }

        if (matchedCommands.Count > 1)
        {
            var commandNames = string.Join(", ", matchedCommands.Select(c => c.Name));
            MessageBox.Show($"Multiple commands specified: {commandNames}\nOnly one command is allowed at a time.", "AppSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return true;
        }

        if (matchedCommands.Count == 1)
        {
            var command = matchedCommands[0];
            logger.LogDebug("Executing command: {Name}", command.Name);
            command.Handler(serviceProvider);
            return true;
        }

        // No commands executed, options applied (or unknown args)
        return false;
    }

    private void ShowHelp()
    {
        var message = new StringBuilder();
        message.AppendLine("AppSwitcher.exe [command] [options]");
        message.AppendLine();

        if (builder.Commands.Count > 0)
        {
            message.AppendLine("Commands:");
            foreach (var cmd in builder.Commands)
            {
                message.AppendLine($"  {cmd.Name,-30} {cmd.Description}");
            }
            message.AppendLine($"  {"--help",-30} Show this help message");
            message.AppendLine();
        }

        if (builder.FlagOptions.Count > 0 || builder.ValuedOptions.Count > 0)
        {
            message.AppendLine("Options:");
            foreach (var opt in builder.FlagOptions)
            {
                message.AppendLine($"  {opt.Name,-30} {opt.Description}");
            }
            foreach (var opt in builder.ValuedOptions)
            {
                message.AppendLine($"  {opt.Name,-30} {opt.Description}");
            }
        }

        MessageBox.Show(message.ToString(), "AppSwitcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}