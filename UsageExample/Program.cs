﻿// This file is part of the ArmoniK project
//
// Copyright (C) ANEO, 2021-2024. All rights reserved.
//   W. Kirschenmann   <wkirschenmann@aneo.fr>
//   J. Gurhem         <jgurhem@aneo.fr>
//   D. Dubuc          <ddubuc@aneo.fr>
//   L. Ziane Khodja   <lzianekhodja@aneo.fr>
//   F. Lemaitre       <flemaitre@aneo.fr>
//   S. Djebbar        <sdjebbar@aneo.fr>
//   J. Fonseca        <jfonseca@aneo.fr>
//   D. Brasseur       <dbrasseur@aneo.fr>
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY, without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.


using System.Text;
using ArmoniK.Extension.CSharp.Client;
using ArmoniK.Extension.CSharp.Client.Common;
using ArmoniK.Extension.CSharp.Client.Common.Domain.Blob;
using ArmoniK.Extension.CSharp.Client.Common.Domain.Task;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace UsageExample;

internal class Program
{
    private static IConfiguration _configuration;
    private static ILogger<Program> logger_;

    private static async Task Main(string[] args)
    {
        Console.WriteLine("Hello Armonik New Extension !");


        Log.Logger = new LoggerConfiguration().MinimumLevel.Override("Microsoft",
                LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        var factory = new LoggerFactory(new[]
            {
                new SerilogLoggerProvider(Log.Logger)
            },
            new LoggerFilterOptions().AddFilter("Grpc",
                LogLevel.Error));

        logger_ = factory.CreateLogger<Program>();

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables();

        _configuration = builder.Build();

        var defaultTaskOptions = new TaskConfiguration(
            2,
            1,
            "subtasking",
            TimeSpan.FromHours(1),
            new Dictionary<string, string>
            {
                { "UseCase", "Launch" }
            }
        );

        var props = new Properties(_configuration, defaultTaskOptions, ["subtasking"]);

        var client = new ArmoniKClient(props, factory);

        var sessionService = await client.GetSessionService();

        var session = await sessionService.CreateSessionAsync();

        Console.WriteLine($"sessionId: {session.SessionId}");

        var blobService = await client.GetBlobService();

        var tasksService = await client.GetTasksService();

        var eventsService = await client.GetEventsService();

        var payload = await blobService.CreateBlobAsync(session, "Payload", Encoding.ASCII.GetBytes("Hello"));

        Console.WriteLine($"payloadId: {payload.BlobId}");

        var result = await blobService.CreateBlobMetadataAsync(session, "Result");

        Console.WriteLine($"resultId: {result.BlobId}");

        var task = await tasksService.SubmitTasksAsync(session,
            new List<TaskNode>([
                new TaskNode
                {
                    Payload = payload,
                    ExpectedOutputs = new[] { result }
                }
            ]));

        Console.WriteLine($"taskId: {task.Single().TaskId}");

        await eventsService.WaitForBlobsAsync(session, new List<BlobInfo>([result]));

        var download = await blobService.DownloadBlobAsync(result,
            CancellationToken.None);
        var stringArray = Encoding.ASCII.GetString(download)
            .Split(new[]
                {
                    '\n'
                },
                StringSplitOptions.RemoveEmptyEntries);

        foreach (var returnString in stringArray) Console.WriteLine($"{returnString}");
    }
}