using System;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<NetManager>(_ => new NetManager(new NoopListener()));
            services.AddSingleton<IMessageSender, NetLibMessageSender>();
        }
    }
}