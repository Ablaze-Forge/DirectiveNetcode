using System.Threading.Tasks;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public interface IClientContextSynchronizator
    {
        ClientContextSynchronizationState State { get; }

        TaskCompletionSource<bool> ProcessingTaskCompletionSource { get; }
        bool HasListenerForProcessingEnd { set; }

        ClientContextSynchronizationState MoveNext();
        void MoveToStartState();
    }

    public enum ClientContextSynchronizationState
    {
        NoSynchronizationAvailable,
        TickStart,
        ProcessingMessages,
        TickEnded,
    }
}
