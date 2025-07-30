using System.Diagnostics;
using LiteNetLib;
using Server.Networking;
using Shared.ECS.Simulation;

namespace Server;

public class GameServer : INetEventListener
{
    private readonly World _world;
    private readonly ReplicationManager _replication;
    private readonly NetManager _net;
    private readonly List<NetPeer> _connectedPeers = new();

    private readonly Stopwatch _tickTimer = Stopwatch.StartNew();
    private readonly Stopwatch _replicationTimer = Stopwatch.StartNew();
    private const int TickRateMs = 50;
    private const int ReplicationRateMs = 100;

    public GameServer(World world, EntityReplicator replicator)
    {
        _world = world;
        _replication = replicator;
        _net = new NetManager();
    }

    public void Run()
    {
        while (true)
        {
            _net.PollEvents();

            if (_tickTimer.ElapsedMilliseconds >= TickRateMs)
            {
                _world.Update(TickRateMs / 1000f);
                _tickTimer.Restart();
            }

            if (_replicationTimer.ElapsedMilliseconds >= ReplicationRateMs)
            {
                _replication.SendSnapshotToAll(_connectedPeers);
                _replicationTimer.Restart();
            }

            Thread.Sleep(1); // prevent CPU spin
        }
    }
}