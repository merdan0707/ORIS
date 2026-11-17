using System.Collections.Specialized;
using System.Globalization;

namespace MiniHttpServer.Framework.Routing;

public static class ParameterBinder
{
    public static object[] Bind(
        List<ParameterInstruction> instructions,
        NameValueCollection queryString,
        Dictionary<string, string> formData,
        string? body)
    {
        var parameters = new object[instructions.Count];

        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            string? rawValue = null;

            switch (instr.Source)
            {
                case ParameterSource.Query:
                    rawValue = queryString[instr.Name];
                    break;
                case ParameterSource.Form:
                    formData.TryGetValue(instr.Name, out rawValue);
                    break;
                case ParameterSource.Body:
                    rawValue = body;
                    break;
            }

            if (rawValue != null)
                parameters[i] = Convert.ChangeType(rawValue, instr.Type, CultureInfo.InvariantCulture);
            else if (instr.HasDefaultValue)
                parameters[i] = instr.DefaultValue!;
            else
                throw new ArgumentException($"Missing required parameter: '{instr.Name}' from {instr.Source}");
        }

        return parameters;
    }
}