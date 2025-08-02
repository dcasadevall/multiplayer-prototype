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
            // Register NetLibMessageSender as the default IMessageSender
            services.AddSingleton<IMessageSender, NetLibJsonMessageSender>();

            // Register NetLibMessageSender and its lifecycle interfaces
            services.AddSingleton<NetLibJsonMessageReceiver>();
            services.AddSingleton<IMessageReceiver>(sp => sp.GetRequiredService<NetLibJsonMessageReceiver>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<NetLibJsonMessageReceiver>());
            services.AddSingleton<IInitializable>(sp => sp.GetRequiredService<NetLibJsonMessageReceiver>());

            // Register Networking Client and Server abstractions
            services.AddSingleton<NetLibNetworkingClient>();
            services.AddSingleton<INetworkingClient>(sp => sp.GetRequiredService<NetLibNetworkingClient>());
            services.AddSingleton<IDisposable>(sp => sp.GetRequiredService<NetLibNetworkingClient>());

            services.AddSingleton<INetworkingServer, NetLibNetworkingServer>();

            // Register the NetManager (used by all network dependencies) and the EventBasedNetListener
            services.AddSingleton<EventBasedNetListener>();
            services.AddSingleton<INetEventListener>(sp => sp.GetRequiredService<EventBasedNetListener>());
            services.AddSingleton<NetManager>();
        }
    }
}