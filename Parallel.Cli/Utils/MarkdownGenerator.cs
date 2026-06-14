// Copyright 2026 Entex Interactive

using System.CommandLine;
using System.Text;

namespace Parallel.Cli.Utils
{
    public static class MarkdownGenerator
    {
        public static string Generate(Command root)
        {
            StringBuilder sb = new StringBuilder();
            BuildCommand(root, sb, 0, root.Name);
            return sb.ToString();
        }

        private static void BuildCommand(Command command, StringBuilder sb, int depth, string fullPath)
        {
            // Heading level (#, ##, ###...)
            string heading = new string('#', depth);
            sb.AppendLine($"{heading} {ToTitle(fullPath.Replace("Parallel", string.Empty).Trim())}");
            sb.AppendLine();

            // Usage block
            sb.AppendLine("```");
            sb.Append(fullPath.ToLower());

            foreach (Argument argument in command.Arguments)
            {
                sb.Append($" <{argument.Name}>");
            }

            foreach (Option option in command.Options)
            {
                sb.Append($" [--{option.Name}]");
            }

            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine();

            // Description
            if (!string.IsNullOrWhiteSpace(command.Description))
            {
                sb.AppendLine(command.Description);
                sb.AppendLine();
            }

            // Parameters table (Arguments + Options unified)
            if (command.Arguments.Count > 0 || command.Options.Count > 0)
            {
                sb.AppendLine("| Parameter | Short hand | Type | Required | Description |");
                sb.AppendLine("|-----------|------------|------|----------|-------------|");

                List<string> lines = (from arg in command.Arguments let required = arg.Arity.MinimumNumberOfValues > 0 ? "Yes" : "No" select $"| {arg.Name} | | {GetTypeName(arg.ValueType)} | {required} | {arg.Description} |").ToList();
                lines.AddRange(from option in command.Options let longName = option.Aliases.FirstOrDefault(a => a.StartsWith("--")) ?? $"--{option.Name}" let shortName = string.Join(", ", option.Aliases.Where(a => a.StartsWith("-") && !a.StartsWith("--"))) let required = option.IsRequired ? "Yes" : "No" select $"| {longName.Replace("--", string.Empty)} | {shortName} | {GetTypeName(option.ValueType)} | {required} | {option.Description} |");
                lines.OrderBy(s => s).ToList().ForEach(s => sb.AppendLine(s));
                sb.AppendLine();
            }

            // Subcommands (THIS is the important fix)
            foreach (Command sub in command.Subcommands)
            {
                sb.AppendLine();

                string childPath = $"{fullPath} {sub.Name}";
                Console.WriteLine(childPath);
                BuildCommand(sub, sb, depth + 1, childPath);
            }
        }

        private static string ToTitle(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? value : char.ToUpper(value[0]) + value[1..];
        }

        private static string GetTypeName(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(int)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(double)) return "double";
            if (type == typeof(DateTime)) return "datetime";

            return type.Name;
        }
    }
}