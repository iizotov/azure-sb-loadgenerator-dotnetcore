using System;
using CommandLine;

namespace LoadGeneratorDotnetCore
{
    class Program
    {
        public static int Main(string[] args)
        {
            bool isConsoleRedirected = Console.IsInputRedirected || Console.IsErrorRedirected || Console.IsOutputRedirected;
            int exitCode = 0;
            OrchestratorClass loadOrchestrator;
            dynamic loadGeneratee = null;
            System.Timers.Timer telemetryTimer = new System.Timers.Timer();
            Func<byte[]> func;
            DateTime terminationDT = DateTime.MaxValue;

            ExecutionOptionsClass executionOptions = new ExecutionOptionsClass();

            try
            {
                Parser.Default.ParseArguments<ExecutionOptionsClass>(args)
                    .WithParsed<ExecutionOptionsClass>(parsedOptions =>
                    {
                        executionOptions = parsedOptions;
                        telemetryTimer = new System.Timers.Timer(executionOptions.Checkpoint);
                        if (executionOptions.TerminateAfter > 0)
                        {
                            terminationDT = DateTime.Now.AddSeconds(executionOptions.TerminateAfter);
                        }

                    })
                    .WithNotParsed<ExecutionOptionsClass>(errors =>
                    {
                        Console.WriteLine("Failed to parse command line arguments");
                        foreach (Error error in errors)
                        {
                            Console.WriteLine(error.ToString());
                        }
                        exitCode = -1;
                    });

                switch (executionOptions.Service)
                {
                    case "eh":
                        loadGeneratee = new EHLoadGeneratorClass(
                            executionOptions.ConnectionString,
                            executionOptions.EntityPath
                            );
                        break;
                    case "sb":
                        loadGeneratee = new SBLoadGeneratorClass(
                            executionOptions.ConnectionString,
                            executionOptions.EntityPath
                            );
                        break;
                }
                func = () => { return loadGeneratee.GeneratePayload(executionOptions.GenerateJson, executionOptions.MessageSize); };
                Console.WriteLine(HelperRoutines.GetExecutionSummary(executionOptions, loadGeneratee, func));

                loadOrchestrator = new OrchestratorClass(
                    loadGeneratee, executionOptions.TargetThroughput, executionOptions.MessagesToSend,
                    executionOptions.BatchSize, executionOptions.DryRun, func);

                loadOrchestrator.Start();

                if (executionOptions.Checkpoint > 0)
                {
                    telemetryTimer.Elapsed += (sender, e) =>
                    {
                        Console.WriteLine(loadOrchestrator.GetStatusSnapshot());
                    };
                    telemetryTimer.Start();
                }

                do
                {
                    if (DateTime.Now >= terminationDT)
                    {
                        String dt = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
                        Console.WriteLine($"[{dt}], terminating after {executionOptions.TerminateAfter} seconds...");
                        break;
                    }
                    if (loadOrchestrator.isJobDone)
                    {
                        String dt = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");
                        Console.WriteLine($"[{dt}] Exiting...");
                        break;
                    }
                    if (!isConsoleRedirected && Console.KeyAvailable)
                    {
                        var ch = Console.ReadKey(true).Key;
                        String dt = DateTime.UtcNow.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff");

                        switch (ch)
                        {
                            case ConsoleKey.Escape:
                                Console.WriteLine("Exiting...");
                                loadOrchestrator.Stop();
                                break;

                            case ConsoleKey.Q:
                                executionOptions.TargetThroughput = executionOptions.TargetThroughput + 10;
                                Console.WriteLine($"[{dt}] New target throughput: {executionOptions.TargetThroughput} msg/sec");

                                loadOrchestrator.Stop();
                                loadOrchestrator = null;
                                func = () => { return loadGeneratee.GeneratePayload(executionOptions.GenerateJson, executionOptions.MessageSize); };
                                loadOrchestrator = new OrchestratorClass(
                                    loadGeneratee, executionOptions.TargetThroughput, executionOptions.MessagesToSend,
                                    executionOptions.BatchSize, executionOptions.DryRun, func);
                                loadOrchestrator.Start();

                                break;
                            case ConsoleKey.A:
                                executionOptions.TargetThroughput = executionOptions.TargetThroughput >= 10 ?
                                                    executionOptions.TargetThroughput - 10 :
                                                    0;
                                Console.WriteLine($"[{dt}] New target throughput: {executionOptions.TargetThroughput} msg/sec");

                                loadOrchestrator.Stop();
                                loadOrchestrator = null;
                                func = () => { return loadGeneratee.GeneratePayload(executionOptions.GenerateJson, executionOptions.MessageSize); };
                                loadOrchestrator = new OrchestratorClass(
                                    loadGeneratee, executionOptions.TargetThroughput, executionOptions.MessagesToSend,
                                    executionOptions.BatchSize, executionOptions.DryRun, func);
                                loadOrchestrator.Start();

                                break;
                            case ConsoleKey.W:
                                executionOptions.BatchSize = executionOptions.BatchSize + 10;
                                Console.WriteLine($"[{dt}] New batch size: {executionOptions.BatchSize}");

                                loadOrchestrator.Stop();
                                loadOrchestrator = null;
                                func = () => { return loadGeneratee.GeneratePayload(executionOptions.GenerateJson, executionOptions.MessageSize); };
                                loadOrchestrator = new OrchestratorClass(
                                    loadGeneratee, executionOptions.TargetThroughput, executionOptions.MessagesToSend,
                                    executionOptions.BatchSize, executionOptions.DryRun, func);
                                loadOrchestrator.Start();
                                break;
                            case ConsoleKey.S:
                                executionOptions.BatchSize = executionOptions.BatchSize > 10 ?
                                                        executionOptions.BatchSize - 10 :
                                                        1;
                                Console.WriteLine($"[{dt}] New batch size: {executionOptions.BatchSize}");

                                loadOrchestrator.Stop();
                                loadOrchestrator = null;
                                func = () => { return loadGeneratee.GeneratePayload(executionOptions.GenerateJson, executionOptions.MessageSize); };
                                loadOrchestrator = new OrchestratorClass(
                                    loadGeneratee, executionOptions.TargetThroughput, executionOptions.MessagesToSend,
                                    executionOptions.BatchSize, executionOptions.DryRun, func);
                                loadOrchestrator.Start();
                                break;
                        }
                    }
                    // sit and relax
                } while (true);
            }
            catch (Exception e)
            {
                // Exceptions from all subroutines will bubble up to here
                exitCode = -1;
                Console.WriteLine();
                Console.WriteLine($"Exception: {e.Message}");
            }
            finally
            {
                if (telemetryTimer.Enabled)
                {
                    telemetryTimer.Stop();
                }
                Console.WriteLine();
                Console.WriteLine($"Execution completed, exit code {exitCode}");
            }
            return exitCode;
        }
    }
}
