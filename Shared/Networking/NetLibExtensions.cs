using System;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Shared.Scheduling;

namespace Shared.Networking
{
    public static class NetLibExtensions
    {
        public static DeliveryMethod ToDeliveryMethod(this ChannelType channelType)
        {
            return channelType switch
            {
                ChannelType.ReliableOrdered => DeliveryMethod.ReliableOrdered,
                ChannelType.Unreliable => DeliveryMethod.Unreliable,
                _ => throw new ArgumentOutOfRangeException(nameof(channelType), channelType, null)
            };
        }
    
        public static void RegisterNetLibTypes(this IServiceCollection services)
        {
            // NetManager must be registered by the application.
            // Server will want to use the listener for incoming connections and clients will use it to connect.
            services.AddSingleton<IMessageSender, NetLibMessageSender>();
            
            // Register NetLibMessageSender and its lifecycle interfaces
            services.AddSingleton<NetLibMessageReceiver>();
            services.AddSingleton<IMessageReceiver>(sp => sp.GetRequiredService<NetLibMessageReceiver>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<NetLibMessageReceiver>());
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<NetLibMessageReceiver>());
        }
    }
}