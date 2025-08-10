using Shared.ECS;
using Shared.ECS.Replication;
using System;

namespace Shared.Prediction
{
    /// <summary>
    /// A component wrapper used for client-side prediction and reconciliation.
    /// 
    /// <para>
    /// <b>Purpose:</b> This component holds both the server-authoritative value
    /// for a given component type <typeparamref name="T"/>. It is used for state that is predicted
    /// on the client (such as position, velocity, etc.) and later corrected by the server.
    /// </para>
    /// 
    /// <para>
    /// <b>Usage:</b> On the client, the original component is updated as the player predicts their own state.
    /// When a new authoritative value is received from the server, <see cref="ServerValue"/> is updated and the
    /// client can reconcile or smooth the difference.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of the predicted component (must implement <see cref="IComponent"/>).</typeparam>
    public class PredictedComponent<T> : IComponent where T : IComponent
    {
        /// <summary>
        /// Controls how often the server replicates this predicted component. Flags can be combined.
        /// For example, InitialValue | SomeTicks to send an initial snapshot and then periodic updates.
        /// </summary>
        [Flags]
        public enum ReplicationMode
        {
            /// <summary>
            /// No replication. The client will always derive the local value.
            /// </summary>
            None = 0,

            /// <summary>
            /// Replicate an initial authoritative value on entity creation only.
            /// </summary>
            InitialValue = 1 << 0,

            /// <summary>
            /// Replicate an authoritative value every server tick.
            /// </summary>
            EveryTick = 1 << 1,

            /// <summary>
            /// Replicate periodically according to <see cref="ReplicationTickRate"/>.
            /// </summary>
            SomeTicks = 1 << 2,

            // We serialize as byte
            MaxValue = 1 << 8,
        }

        /// <summary>
        /// The last server-authoritative value for this component.
        /// </summary>
        public T? ServerValue { get; set; }

        /// <summary>
        /// Whether this predicted component has received authoritative data from the server.
        /// </summary>
        public bool HasServerValue => ServerValue != null;

        /// <summary>
        /// The replication frequency mode for this predicted component.
        /// Defaults to <see cref="ReplicationMode.EveryTick"/>.
        /// </summary>
        public ReplicationMode Mode { get; set; } = ReplicationMode.EveryTick;

        /// <summary>
        /// When <see cref="Mode"/> includes <see cref="ReplicationMode.SomeTicks"/>,
        /// the server should replicate this component every N ticks (N = <see cref="ReplicationTickRate"/>).
        /// Defaults to 1.
        /// </summary>
        public uint ReplicationTickRate { get; set; } = 1;

        public uint LastSentAtTick { get; set; } = 0;

        public void Serialize(IComponentWriter writer)
        {
            // Payload
            writer.Put(HasServerValue);
            if (HasServerValue)
                writer.Put(ServerValue!);
        }

        public void Deserialize(IComponentReader reader)
        {
            // Payload
            if (reader.GetBool())
                ServerValue = reader.GetComponent<T>();
        }
    }
}