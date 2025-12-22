using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    public readonly struct ClientToServerSendPipeline<TId, TMessageMetadata, TClientToServerSendStep> : IMessagePipeline<TClientToServerSendStep, MessageSendParams<TId, TMessageMetadata>>
        where TId : unmanaged, IEquatable<TId>
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
        where TClientToServerSendStep : unmanaged, IClientToServerSendStep<TId, TMessageMetadata>
    {
        private readonly NativeList<TClientToServerSendStep> m_Steps;

        public ClientToServerSendPipeline(params TClientToServerSendStep[] steps)
        {
            m_Steps = new();

            AddSteps(steps);
        }

        public void AddStep(TClientToServerSendStep step)
        {
            m_Steps.Add(step);
        }

        public void AddSteps(params TClientToServerSendStep[] steps)
        {
            NativeArray<TClientToServerSendStep> stepArray = new(steps, Allocator.Temp);

            m_Steps.AddRange(stepArray);
        }

        public PipelineResult ExecuteSteps(MessageSendParams<TId, TMessageMetadata> messageParams)
        {
            foreach (TClientToServerSendStep step in m_Steps)
            {
                PipelineStepResult stepResult = step.Execute(messageParams);

                if (stepResult == PipelineStepResult.DisconnectClient)
                {
                    return PipelineResult.DisconnectClient;
                }

                if (stepResult != PipelineStepResult.Success)
                {
                    return PipelineResult.DiscardMessage;
                }
            }

            return PipelineResult.Success;
        }
    }
}
