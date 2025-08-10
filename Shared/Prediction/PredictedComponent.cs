using Shared.ECS;
using Shared.ECS.Replication;

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
        /// The last server-authoritative value for this component.
        /// </summary>
        public T? ServerValue { get; set; }

        /// <summary>
        /// Whether this predicted component has received authoritative data from the server.
        /// </summary>
        public bool HasServerValue => ServerValue != null;

        public void Serialize(IComponentWriter writer)
        {
            writer.Put(HasServerValue);
            if (HasServerValue)
                writer.Put(ServerValue!);
        }

        public void Deserialize(IComponentReader reader)
        {
            if (reader.GetBool())
                ServerValue = reader.GetComponent<T>();
        }
    }
}