using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Aevatar.Notification;

public interface INotificationHandlerFactory : ISingletonDependency
{
    NotificationWrapper? GetNotification(NotificationTypeEnum type);
}

public class NotificationProcessorFactory : INotificationHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationProcessorFactory> _logger;
    private readonly Dictionary<NotificationTypeEnum, Type> _typeDic = new Dictionary<NotificationTypeEnum, Type>();
    private readonly Dictionary<Type, Type> _typeToGeneratorDic = new Dictionary<Type, Type>();

    public NotificationProcessorFactory(IServiceProvider serviceProvider, ILogger<NotificationProcessorFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;

        var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                t.GetInterfaces().Any(item =>
                    item.IsGenericType && item.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
            );


        foreach (var item in handlerTypes)
        {
            var instance =
                ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, item) as INotificationHandlerType;
            if (instance == null)
            {
                _logger.LogError(
                    $"[NotificationProcessor] Constructor ActivatorUtilities.GetServiceOrCreateInstance error:{item.FullName}");
                continue;
            }

            _typeDic.TryAdd(instance.Type, item);
            
            var inheritInterface  = item.GetInterfaces().First(w => w.IsGenericType && w.GetGenericTypeDefinition() == typeof(INotificationHandler<>));
            var genericType = inheritInterface.GetGenericArguments()[0];
            _typeToGeneratorDic.TryAdd(item, genericType);
        }
    }

    public NotificationWrapper? GetNotification(NotificationTypeEnum type)
    {
        var (handlerType, instance) = GetHandlerInstance(type);
        if (instance == null)
        {
            return null;
        }

        if (_typeToGeneratorDic.TryGetValue(handlerType, out var generatorType) == false)
        {
            return null;
        }

        return new NotificationWrapper(handlerType, generatorType, instance,
            _serviceProvider.GetRequiredService<ILogger<NotificationWrapper>>());
    }

    private Tuple<Type, object?> GetHandlerInstance(NotificationTypeEnum type)
    {
        if (_typeDic.TryGetValue(type, out var handlerType) == false)
        {
            _logger.LogError(
                $"[NotificationProcessor] GetNotificationMessage NotificationTypeEnum not found:{type.ToString()}");
            return new Tuple<Type, object?>(null, null);
        }

        return new Tuple<Type, object?>(handlerType,
            ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, handlerType));
    }
}