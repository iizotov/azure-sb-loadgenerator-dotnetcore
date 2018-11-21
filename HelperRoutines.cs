using System;
using System.Text;

namespace LoadGeneratorDotnetCore
{
    class HelperRoutines
    {
        public static string GetExecutionSummary(ExecutionOptionsClass executionOptions,
            dynamic loadGeneratee, Func<byte[]> loadGenerator)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("-----INITIAL PARAMETERS-----");
            summary.AppendLine($"Execution started: { DateTime.Now.ToUniversalTime() }");
            summary.AppendLine(ReturnAllProperties(executionOptions));
            summary.AppendLine($"Sample payload: { Encoding.UTF8.GetString(loadGenerator()) }");
            summary.AppendLine("-----INTERACTIVITY-----");
            summary.AppendLine("Press Q/A to increase/decrease the target throughput");
            summary.AppendLine("Press W/S to increase/decrease batch size");
            summary.AppendLine("Press Escape to cancel all threads and gracefully exit");
            summary.AppendLine("-----EXECUTION LOG FOLLOWS-----");
            return summary.ToString();
        }
        private static string ReturnAllProperties(object obj)
        {
            string ret = "";
            foreach (var prop in obj.GetType().GetProperties())
            {
                ret += $"{prop.Name}: {prop.GetValue(obj)}\n";
            }
            return ret;
        }
    }
}