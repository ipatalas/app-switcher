namespace AppSwitcher.CLI;

internal class CliBuilder
{
    private readonly List<CommandRegistration> _commands = [];
    private readonly List<FlagOptionRegistration> _flagOptions = [];
    private readonly List<ValuedOptionRegistration> _valuedOptions = [];

    internal IReadOnlyList<CommandRegistration> Commands => _commands;
    internal IReadOnlyList<FlagOptionRegistration> FlagOptions => _flagOptions;
    internal IReadOnlyList<ValuedOptionRegistration> ValuedOptions => _valuedOptions;

    public CliBuilder AddCommand(string name, string description, Action<IServiceProvider> handler)
    {
        _commands.Add(new CommandRegistration(name, description, handler));
        return this;
    }

    public CliBuilder AddOption(string name, string description, Action<CliOptions> handler)
    {
        _flagOptions.Add(new FlagOptionRegistration(name, description, handler));
        return this;
    }

    public CliBuilder AddOption(string name, string description, Action<CliOptions, string> handler)
    {
        _valuedOptions.Add(new ValuedOptionRegistration(name, description, handler));
        return this;
    }

    internal record CommandRegistration(string Name, string Description, Action<IServiceProvider> Handler);
    internal record FlagOptionRegistration(string Name, string Description, Action<CliOptions> Handler);
    internal record ValuedOptionRegistration(string Name, string Description, Action<CliOptions, string> Handler);
}