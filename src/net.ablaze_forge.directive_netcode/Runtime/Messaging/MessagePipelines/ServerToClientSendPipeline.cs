using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    public readonly struct ServerToClientSendPipeline<TId, TMessageMetadata, TServerToClientSendStep> : IMessagePipeline<TServerToClientSendStep, MessageSendParams<TId, TMessageMetadata>>
        where TId : unmanaged, IEquatable<TId>
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamSerializable
        where TServerToClientSendStep : unmanaged, IServerToClientSendStep<TId, TMessageMetadata>
    {
        private readonly NativeList<TServerToClientSendStep> m_Steps;

        public ServerToClientSendPipeline(params TServerToClientSendStep[] steps) 
        {
            m_Steps = new();

            AddSteps(steps);
        }

        public void AddStep(TServerToClientSendStep step)
        {
            m_Steps.Add(step);
        }

        public void AddSteps(params TServerToClientSendStep[] steps)
        {
            NativeArray<TServerToClientSendStep> stepArray = new(steps, Allocator.Temp);

            m_Steps.AddRange(stepArray);
        }

        public PipelineResult ExecuteSteps(MessageSendParams<TId, TMessageMetadata> messageParams)
        {
            foreach (TServerToClientSendStep step in m_Steps)
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
