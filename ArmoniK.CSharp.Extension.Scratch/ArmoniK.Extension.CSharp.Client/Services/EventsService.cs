﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ArmoniK.Api.Client;
using ArmoniK.Api.gRPC.V1;
using ArmoniK.Api.gRPC.V1.Events;
using ArmoniK.Extension.CSharp.Client.Common.Domain;
using ArmoniK.Extension.CSharp.Client.Common.Services;
using ArmoniK.Utils;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace ArmoniK.Extension.CSharp.Client.Services;

public class EventsService : IEventsService
{
    private readonly ObjectPool<ChannelBase> _channel;

    private readonly ILogger<EventsService> _logger;

    public EventsService(ObjectPool<ChannelBase> channel, ILoggerFactory loggerFactory)
    {
        _channel = channel;
        _logger = loggerFactory.CreateLogger<EventsService>();
    }

    public Task WaitForBlobsAsync(ICollection<string> blobIds, Session session,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task WaitForBlobsAsync(ICollection<BlobInfo> blobInfos, Session session,
        CancellationToken cancellationToken = default)
    {
        await using var channel = await _channel.GetAsync(cancellationToken).ConfigureAwait(false);
        var eventsClient = new Events.EventsClient(channel);
        await eventsClient.WaitForResultsAsync(session.Id,
            blobInfos.Select(x => x.BlobId).ToList(),
            cancellationToken);
    }
}