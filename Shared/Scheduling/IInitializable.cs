namespace Shared.Scheduling
{
    /// <summary>
    /// Represents an object that requires initialization at application or system startup.
    /// 
    /// Implementations of this interface can be registered with a startup or dependency injection system
    /// to have their <see cref="Initialize"/> method called automatically at the appropriate time.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Called once to initialize the object.
        /// </summary>
        void Initialize();
    }
}