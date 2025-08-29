using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Aevatar.Core;
using Aevatar.GAgents.AElf.Agent.Event;
using Aevatar.GAgents.AElf.Agent.Events;
using Aevatar.GAgents.AElf.Agent.GEvents;
using Aevatar.GAgents.AElf.Agent.Grains;
using Aevatar.GAgents.AElf.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Providers;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;

namespace Aevatar.GAgents.AElf.Agent;

[Description("Comprehensive AElf blockchain agent that handles wallet management, transaction execution, smart contract deployment and interaction, and blockchain state monitoring with enterprise-grade security.")]
[StorageProvider(ProviderName = "PubSubStore")]
[LogConsistencyProvider(ProviderName = "LogStorage")]
[GAgent(nameof(AElfGAgent))]
public class AElfGAgent : GAgentBase<AElfAgentGState, TransactionStateLogEvent>, IAElfAgent
{
    public AElfGAgent(ILogger<AElfGAgent> logger)
    {
    }

    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("AElf Blockchain Agent");
    }

    [EventHandler]
    protected async Task ExecuteAsync(CreateTransactionGEvent gGEventData)
    {
        var createTransactionStateLogEvent = new CreateTransactionStateLogEvent
        {
            ChainId = gGEventData.ChainId,
            SenderName = gGEventData.SenderName,
            ContractAddress = gGEventData.ContractAddress,
            MethodName = gGEventData.MethodName,
        };
        RaiseEvent(createTransactionStateLogEvent);
        await ConfirmEvents();
        _ = GrainFactory.GetGrain<ITransactionGrain>(createTransactionStateLogEvent.Id).SendAElfTransactionAsync(
            new SendTransactionDto
            {
                Id = createTransactionStateLogEvent.Id,
                ChainId = gGEventData.ChainId,
                SenderName = gGEventData.SenderName,
                ContractAddress = gGEventData.ContractAddress,
                MethodName = gGEventData.MethodName,
                Param = gGEventData.Param
            });
        Logger.LogInformation("ExecuteAsync: AElf {MethodName}", gGEventData.MethodName);
    }

    [EventHandler]
    public Task ExecuteAsync(SendTransactionCallBackGEvent gGEventData)
    {
        RaiseEvent(new SendTransactionStateLogEvent
        {
            CreateTransactionGEventId = gGEventData.CreateTransactionGEventId,
            ChainId = gGEventData.ChainId,
            TransactionId = gGEventData.TransactionId
        });

        _ = GrainFactory.GetGrain<ITransactionGrain>(gGEventData.Id).LoadAElfTransactionResultAsync(
            new QueryTransactionDto
            {
                CreateTransactionGEventId = gGEventData.CreateTransactionGEventId,
                ChainId = gGEventData.ChainId,
                TransactionId = gGEventData.TransactionId
            });
        return Task.CompletedTask;
    }

    [EventHandler]
    public async Task ExecuteAsync(QueryTransactionCallBackGEvent gGEventData)
    {
        if (gGEventData.IsSuccess)
        {
            RaiseEvent(new TransactionSuccessStateLogEvent
            {
                CreateTransactionGEventId = gGEventData.CreateTransactionGEventId
            });
        }
        else
        {
            RaiseEvent(new TransactionFailedStateLogEvent()
            {
                CreateTransactionGEventId = gGEventData.CreateTransactionGEventId,
                Error = gGEventData.Error
            });
        }

        await ConfirmEvents();
    }

    public async Task ExecuteTransactionAsync(CreateTransactionGEvent gGEventData)
    {
        await ExecuteAsync(gGEventData);
    }

    public async Task<AElfAgentGState> GetAElfAgentDto()
    {
        AElfAgentDto aelfAgentDto = new AElfAgentDto();
        aelfAgentDto.Id = State.Id;
        aelfAgentDto.PendingTransactions = State.PendingTransactions;
        return aelfAgentDto;
    }

    protected Task ExecuteAsync(TransactionStateLogEvent eventData)
    {
        return Task.CompletedTask;
    }

    protected override void GAgentTransitionState(AElfAgentGState state, StateLogEventBase<TransactionStateLogEvent> @event)
    {
        switch (@event)
        {
            case TransactionFailedStateLogEvent transactionFailedStateLogEvent:
                state.PendingTransactions.Remove(transactionFailedStateLogEvent.CreateTransactionGEventId);
                break;
            
            case CreateTransactionStateLogEvent createTransactionStateLogEvent:
                if (state.Id == Guid.Empty)
                {
                    state.Id = Guid.NewGuid();
                }
                state.PendingTransactions[createTransactionStateLogEvent.Id] = createTransactionStateLogEvent;
                break;
            case SendTransactionStateLogEvent sendTransactionStateLogEvent:
                state.PendingTransactions[sendTransactionStateLogEvent.CreateTransactionGEventId].TransactionId =
                    sendTransactionStateLogEvent.TransactionId;
                break;
            case TransactionSuccessStateLogEvent transactionSuccessStateLogEvent:
                state.PendingTransactions.Remove(transactionSuccessStateLogEvent.CreateTransactionGEventId);
                break;
        }
    }
}

public interface IAElfAgent : IGrainWithGuidKey
{
    Task ExecuteTransactionAsync(CreateTransactionGEvent gGEventData);
    Task<AElfAgentGState> GetAElfAgentDto();
}