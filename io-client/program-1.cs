using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

// Make sure to include the namespace for your TaskMaster and concrete task types.
// using MyNamespace;

namespace TaskMasterDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Run our demo.
            await RunTaskMasterDemoAsync();
        }

        private static async Task RunTaskMasterDemoAsync()
        {
            // Create an HttpClient to be shared among the HTTP tasks.
            HttpClient httpClient = new HttpClient();

            // Dummy connection string for SQL tasks.
            string connectionString = "YourConnectionStringHere";

            // Create a heterogeneous list of I/O-bound tasks.
            List<IIoBoundTask> tasks = new List<IIoBoundTask>();

            // --- HTTP Tasks ---
            // Two calls to Service1 and one call to Service2.
            tasks.Add(new HttpTask(httpClient, "https://api.service1.com/endpoint1"));
            tasks.Add(new HttpTask(httpClient, "https://api.service1.com/endpoint2"));
            tasks.Add(new HttpTask(httpClient, "https://api.service2.com/endpoint1"));

            // --- SQL Tasks ---
            // Five SQL commands querying four different tables.
            tasks.Add(new SqlCommandTask(connectionString, "SELECT * FROM Table1", System.Data.CommandType.Text));
            tasks.Add(new SqlCommandTask(connectionString, "SELECT * FROM Table2", System.Data.CommandType.Text));
            tasks.Add(new SqlCommandTask(connectionString, "SELECT * FROM Table3", System.Data.CommandType.Text));
            tasks.Add(new SqlCommandTask(connectionString, "SELECT * FROM Table4", System.Data.CommandType.Text));
            // A second command targeting Table1.
            tasks.Add(new SqlCommandTask(connectionString, "SELECT * FROM Table1", System.Data.CommandType.Text));

            // Cancellation token; for the demo we simply use CancellationToken.None.
            CancellationToken cancellationToken = CancellationToken.None;

            // Set up a simple console logger.
            using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "hh:mm:ss ";
                });
            });
            ILogger logger = loggerFactory.CreateLogger("TaskMasterDemo");

            // Define a delegate to process the aggregated IoResults.
            // For demonstration, we count the total tasks, number of successes, and failures.
            string ProcessResults(IEnumerable<IoResult> results)
            {
                int total = results.Count();
                int successes = results.Count(r => r.IsSuccess);
                int failures = total - successes;
                return $"Total tasks: {total}, Successes: {successes}, Failures: {failures}";
            }

            // Execute all tasks concurrently using the TaskMaster, capturing performance statistics.
            (string processResult, DataTable stats) =
                await TaskMaster.ExecuteTasksWithStats<string>(
                    tasks,
                    ProcessResults,
                    cancellationToken,
                    logger);

            // Output the processing result.
            Console.WriteLine("Processing Result:");
            Console.WriteLine(processResult);
            Console.WriteLine();

            // Output the execution statistics.
            Console.WriteLine("Execution Statistics:");
            foreach (DataRow row in stats.Rows)
            {
                Console.WriteLine(
                    $"{row["TaskType"],-40} Start: {row["StartTime"],-25} End: {row["EndTime"],-25} Elapsed: {row["ElapsedTime"]}");
            }
        }
    }
}
