namespace Shared.Scheduling
{
    /// <summary>
    /// Represents an object that can be "ticked" (updated) every frame or at a fixed interval.
    /// 
    /// Implementations of this interface can be registered with <c>ServiceCollection.AddSingleton()</c>
    /// to be automatically ticked by the scheduler.
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// Called once per tick to update the object.
        /// </summary>
        void Tick();
    }
}