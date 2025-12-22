using System.Threading.Tasks;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public class DefaultServerContextSynchronizator : IServerContextSynchronizator
    {
        public ServerContextSynchronizationState State { get; private set; } = ServerContextSynchronizationState.NoSynchronizationAvailable;

        public TaskCompletionSource<bool> ProcessingTaskCompletionSource { get; private set; } = new();
        public bool HasListenerForProcessingEnd { private get; set; }

        public void MoveToStartState()
        {
            if (ProcessingTaskCompletionSource == null || ProcessingTaskCompletionSource.Task.IsCompleted)
            {
                ProcessingTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            State = ServerContextSynchronizationState.TickStart;
        }

        public ServerContextSynchronizationState MoveNext()
        {
            switch (State)
            {
                case ServerContextSynchronizationState.TickStart:
                    State = ServerContextSynchronizationState.CleanningConnections;
                    break;
                case ServerContextSynchronizationState.CleanningConnections:
                    State = ServerContextSynchronizationState.AddingNewConnections;
                    break;
                case ServerContextSynchronizationState.AddingNewConnections:
                    State = ServerContextSynchronizationState.ProcessingMessages;
                    break;
                case ServerContextSynchronizationState.ProcessingMessages:
                    State = ServerContextSynchronizationState.AuthenticationRequests;
                    break;
                case ServerContextSynchronizationState.AuthenticationRequests:
                    if (HasListenerForProcessingEnd)
                    {
                        HasListenerForProcessingEnd = false;
                        _ = ProcessingTaskCompletionSource?.TrySetResult(true);
                    }
                    State = ServerContextSynchronizationState.TickEnded;
                    break;
            }

            return State;
        }
    }
}
