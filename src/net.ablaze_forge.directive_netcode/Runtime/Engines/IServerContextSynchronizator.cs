using System.Threading.Tasks;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public interface IServerContextSynchronizator
    {
        ServerContextSynchronizationState State { get; }

        TaskCompletionSource<bool> ProcessingTaskCompletionSource { get; }

        public bool HasListenerForProcessingEnd {set;}

        ServerContextSynchronizationState MoveNext();
        void MoveToStartState();
    }

    public enum ServerContextSynchronizationState
    {
        NoSynchronizationAvailable,
        TickStart,
        CleanningConnections,
        AddingNewConnections,
        AuthenticationRequests,
        ProcessingMessages,
        TickEnded,
    }
}
