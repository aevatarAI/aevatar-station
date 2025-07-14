using System;
using System.Threading.Tasks;
using AutoGen.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.Notification;

public class NotificationWrapper
{
    private readonly Type _handlerType;
    private readonly Type _generatorType;
    private object _handlerInstance;
    private readonly ILogger<NotificationWrapper> _logger;

    public NotificationWrapper(Type handlerType, Type generatorType, object handlerInstance,
        ILogger<NotificationWrapper> logger)
    {
        _handlerType = handlerType;
        _generatorType = generatorType;
        _handlerInstance = handlerInstance;
        _logger = logger;
    }

    public async Task<bool> CheckAuthorizationAsync(string input, Guid? creatorId)
    {
        var convertObj = GetHandlerParameter(input);
        if (convertObj == null)
        {
            return false;
        }

        var parameters = new object[] { convertObj, creatorId };
        var getContentMethod = _handlerType.GetMethod("CheckAuthorizationAsync");
        if (getContentMethod == null)
        {
            _logger.LogError(
                $"[NotificationHandlerFactory] CheckAuthorizationAsync ConvertInput method not found:{_handlerType.FullName}");
            return false;
        }

        var response = (Task<bool>)getContentMethod.Invoke(_handlerInstance, parameters)!;

        return await response;
    }

    public object? ConvertInput(string input)
    {
        return GetHandlerParameter(input);
    }

    public async Task<string?> GetNotificationMessage(object? parameter)
    {
        var convertParameter = ConvertParameter(parameter);
        var parameters = new object?[] { convertParameter };
        var getContentMethod = _handlerType.GetMethod("GetContentForShowAsync");
        if (getContentMethod == null)
        {
            _logger.LogError(
                $"[NotificationHandlerFactory] GetNotificationMessage ConvertInput method not found:{_handlerType.FullName}");
            return null;
        }

        var response = (Task<string>)getContentMethod.Invoke(_handlerInstance, parameters)!;
        return await response;
    }

    public async Task ProcessNotificationAsync(object? parameter, NotificationStatusEnum status)
    {
        var convertParameter = ConvertParameter(parameter);
        var parameters = new object?[] { convertParameter };
        switch (status)
        {
            case NotificationStatusEnum.None:
                break;
            case NotificationStatusEnum.Agree:
                await ExecuteMethod("HandleAgreeAsync", parameters);
                break;
            case NotificationStatusEnum.Refuse:
                await ExecuteMethod("HandleRefuseAsync", parameters);
                break;
            case NotificationStatusEnum.Read:
                await ExecuteMethod("HandleReadAsync", parameters);
                break;
            case NotificationStatusEnum.Ignore:
                await ExecuteMethod("HandleIgnoreAsync", parameters);
                break;
            case NotificationStatusEnum.Withdraw:
                await ExecuteMethod("HandleWithdrawAsync", parameters);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
        }
    }

    private async Task ExecuteMethod(string methodName, object[] parameter)
    {
        var method = _handlerType.GetMethod(methodName);
        if (method == null)
        {
            _logger.LogError(
                $"[NotificationHandlerFactory] ExecuteMethod method not found:{_handlerType.FullName}, method:{methodName}");
            return;
        }

        var task = (Task)method.Invoke(_handlerInstance, parameter);
        await task;
    }

    private object? GetHandlerParameter(string input)
    {
        var method = _handlerType.GetMethod("ConvertInput");
        if (method == null)
        {
            _logger.LogError(
                $"[NotificationHandlerFactory] GetHandlerParameter ConvertInput method == null:{_handlerType.FullName}");
            return null;
        }

        var convertObj = method.Invoke(_handlerInstance, new object[] { input });
        if (convertObj == null)
        {
            _logger.LogError(
                $"[NotificationHandlerFactory] GetHandlerParameter ConvertInput convertObj == null :{_handlerType.FullName}");
        }

        return convertObj;
    }

    private object? ConvertParameter(object? parameter)
    {
        if (parameter == null)
        {
            return null;
        }

        if (parameter.GetType() == _generatorType)
        {
            return parameter;
        }

        var jsonStr = JsonConvert.SerializeObject(parameter);
        return JsonConvert.DeserializeObject(jsonStr, _generatorType);
    }
}