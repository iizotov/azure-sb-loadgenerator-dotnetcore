using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGeneratorDotnetCore
{
    class HelperRoutines
    {
        public static string GetExecutionSummary(ExecutionOptionsClass executionOptions, 
            dynamic loadGeneratee)
        {
            StringBuilder summary = new StringBuilder();
            summary.AppendLine("-----INITIAL PARAMETERS-----");
            summary.AppendLine($"Execution started: {DateTime.Now.ToUniversalTime()}");
            summary.AppendLine(ReturnAllProperties(executionOptions));
            summary.AppendLine($"Sample payload: {Encoding.UTF8.GetString(loadGeneratee.GeneratePayload())}");
            summary.AppendLine("-----INTERACTIVITY-----");
            summary.AppendLine("Press Q/A to increase/decrease the number of threads");
            summary.AppendLine("Press W/S to increase/decrease payload size");
            summary.AppendLine("Press E/D to increase/decrease batch size");
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