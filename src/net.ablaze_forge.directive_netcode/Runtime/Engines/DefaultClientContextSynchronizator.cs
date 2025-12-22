using System.Threading.Tasks;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public class DefaultClientContextSynchronizator : IClientContextSynchronizator
    {
        public ClientContextSynchronizationState State { get; private set; } = ClientContextSynchronizationState.NoSynchronizationAvailable;

        public TaskCompletionSource<bool> ProcessingTaskCompletionSource { get; private set; } = new();
        public bool HasListenerForProcessingEnd { private get; set; }

        public void MoveToStartState()
        {
            if(ProcessingTaskCompletionSource is null || ProcessingTaskCompletionSource.Task.IsCompleted)
            {
                ProcessingTaskCompletionSource = new();
            }

            State = ClientContextSynchronizationState.TickStart;
        }

        public ClientContextSynchronizationState MoveNext()
        {
            switch (State)
            {
                case ClientContextSynchronizationState.TickStart:
                    State = ClientContextSynchronizationState.ProcessingMessages;
                    break;
                case ClientContextSynchronizationState.ProcessingMessages:
                    if (HasListenerForProcessingEnd)
                    {
                        HasListenerForProcessingEnd = false;
                        _ = ProcessingTaskCompletionSource.TrySetResult(true);
                    }
                    State = ClientContextSynchronizationState.TickEnded;
                    break;
            }

            return State;
        }
    }
}
