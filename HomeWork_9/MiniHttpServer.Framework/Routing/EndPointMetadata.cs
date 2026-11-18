using System.Reflection;

namespace MiniHttpServer.Framework.Routing;

public record EndPointMetadata(
    Type ControllerType,
    MethodInfo Method,
    string HttpMethod,
    List<ParameterInstruction> ParameterInstructions);