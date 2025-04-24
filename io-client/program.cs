using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TaskMasterConsoleDemo
{
    class Program
    {
        // Async Main is supported when you return Task or Task<int>
        static async Task Main(string[] args)
        {
            // 1. Prepare tasks (reuse the example from before)
            HttpClient httpClient = new HttpClient();
            string connStr = "YourConnectionStringHere";

            var tasks = new List<IIoBoundTask>
            {
                new HttpTask(httpClient, "https://api.service1.com/endpoint1"),
                new HttpTask(httpClient, "https://api.service1.com/endpoint2"),
                new HttpTask(httpClient, "https://api.service2.com/endpoint1"),

                new SqlCommandTask(connStr, "SELECT * FROM Table1"),
                new SqlCommandTask(connStr, "SELECT * FROM Table2"),
                new SqlCommandTask(connStr, "SELECT * FROM Table3"),
                new SqlCommandTask(connStr, "SELECT * FROM Table4"),
                new SqlCommandTask(connStr, "SELECT * FROM Table1")
            };

            // 2. Logger
            using var loggerFactory = LoggerFactory.Create(builder =>
                builder.AddSimpleConsole());
            ILogger logger = loggerFactory.CreateLogger<Program>();

            // 3. Define processing delegate
            string ProcessResults(IEnumerable<IoResult> results)
            {
                int total = results.Count();
                int ok    = results.Count(r => r.IsSuccess);
                return $"Total={total}, Success={ok}, Failures={total - ok}";
            }

            // 4. Run and await
            var (summary, stats) = await TaskMaster
                .ExecuteTasksWithStats<string>(
                    tasks,
                    ProcessResults,
                    CancellationToken.None,
                    logger);

            // 5. Output
            Console.WriteLine("Summary: " + summary);
            Console.WriteLine("Stats:");
            foreach (DataRow row in stats.Rows)
            {
                Console.WriteLine(
                  $"{row["TaskType"],-30} {row["ElapsedTime"]}");
            }
        }
    }
}
