namespace MiniHttpServer.Framework.Routing;

public record ParameterInstruction(
    string Name,
    Type Type,
    ParameterSource Source,
    bool HasDefaultValue,
    object? DefaultValue);