using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.EventHubs;

/*
TODO: add PID controller and ability to specify target rate
*/

namespace LoadGeneratorDotnetCore
{
    class Program
    {
        public static int Main(string[] args)
        {
            int exitCode = -1;
            OrchestratorClass loadOrchestrator;
            dynamic loadGeneratee = null;

            ExecutionOptionsClass executionOptions = new ExecutionOptionsClass();

            try
            {
                Parser.Default.ParseArguments<ExecutionOptionsClass>(args)
                    .WithParsed<ExecutionOptionsClass>(parsedOptions =>
                    {
                        executionOptions = parsedOptions;
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
                            executionOptions.ConnectionString, executionOptions.EntityPath,
                            executionOptions.MessageSize, executionOptions.GenerateJson,
                            executionOptions.BatchSize
                            );
                        break;
                    case "sb":
                        loadGeneratee = new SBLoadGeneratorClass(
                            executionOptions.ConnectionString, executionOptions.EntityPath,
                            executionOptions.MessageSize, executionOptions.GenerateJson,
                            executionOptions.BatchSize
                            );
                        break;
                }
                Console.WriteLine(HelperRoutines.GetExecutionSummary(executionOptions, loadGeneratee));
                loadOrchestrator = new OrchestratorClass(loadGeneratee, executionOptions.Threads, executionOptions.MessagesToSend);
                loadOrchestrator.Start();

                var progressTimer = new System.Timers.Timer { Interval = executionOptions.Checkpoint };
                progressTimer.Elapsed += (sender, e) => { loadOrchestrator.PrintProgress(); };
                progressTimer.Start();

                do
                {
                    if (loadOrchestrator.isJobDone)
                    {
                        progressTimer.Stop();
                        break;
                    }
                    if (Console.KeyAvailable)
                    {
                        var ch = Console.ReadKey(true).Key;
                        switch (ch)
                        {
                            case ConsoleKey.Escape:
                                loadOrchestrator.SetTargetThreadCount(0);
                                break;
                            case ConsoleKey.Q:
                                loadOrchestrator.SetTargetThreadCount(loadOrchestrator.GetTargetThreadCount() + 1);
                                break;
                            case ConsoleKey.A:
                                loadOrchestrator.SetTargetThreadCount(loadOrchestrator.GetTargetThreadCount() - 1);
                                break;
                            case ConsoleKey.W:
                                loadOrchestrator.SetBatchSize(loadOrchestrator.GetBatchSize() + 10);
                                break;
                            case ConsoleKey.S:
                                loadOrchestrator.SetBatchSize(loadOrchestrator.GetBatchSize() - 10);
                                break;
                            case ConsoleKey.E:
                                loadOrchestrator.SetPayloadSize(loadOrchestrator.GetPayloadSize() + 10);
                                break;
                            case ConsoleKey.D:
                                loadOrchestrator.SetPayloadSize(loadOrchestrator.GetPayloadSize() - 10);
                                break;
                        }
                    }
                    Thread.Sleep(300);
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
                Console.WriteLine();
                Console.WriteLine($"Execution completed, exit code {exitCode}");
            }
            return exitCode;
        }
    }
}
