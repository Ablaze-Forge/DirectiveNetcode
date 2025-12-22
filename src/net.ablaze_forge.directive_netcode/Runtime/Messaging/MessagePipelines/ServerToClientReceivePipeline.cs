using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    public readonly struct ServerToClientReceivePipeline<TId, TMessageMetadata, TServerToClientReceiveStep> : IMessagePipeline<TServerToClientReceiveStep, MessageReceiveParams<TId, TMessageMetadata>>
        where TId : unmanaged, IEquatable<TId>
        where TServerToClientReceiveStep : unmanaged, IServerToClientReceiveStep<TId, TMessageMetadata>
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamDeserializable<TMessageMetadata>
    {
        private readonly NativeList<TServerToClientReceiveStep> m_Steps;

        public ServerToClientReceivePipeline(params TServerToClientReceiveStep[] steps)
        {
            m_Steps = new();

            AddSteps(steps);
        }

        public void AddStep(TServerToClientReceiveStep step)
        {
            m_Steps.Add(step);
        }

        public void AddSteps(params TServerToClientReceiveStep[] steps)
        {
            NativeArray<TServerToClientReceiveStep> stepArray = new(steps, Allocator.Temp);

            m_Steps.AddRange(stepArray);
        }

        public PipelineResult ExecuteSteps(MessageReceiveParams<TId, TMessageMetadata> messageParams)
        {
            foreach (TServerToClientReceiveStep step in m_Steps)
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
