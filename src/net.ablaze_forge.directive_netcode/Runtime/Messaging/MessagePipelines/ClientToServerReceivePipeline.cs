using System;
using Unity.Collections;

namespace AblazeForge.DirectiveNetcode.Messaging.Pipelines
{
    public readonly struct ClientToServerReceivePipeline<TId, TMessageMetadata, TClientToServerReceiveStep> : IMessagePipeline<TClientToServerReceiveStep, MessageReceiveParams<TId, TMessageMetadata>>
        where TId : unmanaged, IEquatable<TId>
        where TClientToServerReceiveStep : unmanaged, IClientToServerReceiveStep<TId, TMessageMetadata>
        where TMessageMetadata : unmanaged, IMessageMetadata, IDataStreamDeserializable<TMessageMetadata>
    {
        private readonly NativeList<TClientToServerReceiveStep> m_Steps;

        public ClientToServerReceivePipeline(params TClientToServerReceiveStep[] steps)
        {
            m_Steps = new();

            AddSteps(steps);
        }

        public void AddStep(TClientToServerReceiveStep step)
        {
            m_Steps.Add(step);
        }

        public void AddSteps(params TClientToServerReceiveStep[] steps)
        {
            NativeArray<TClientToServerReceiveStep> stepArray = new(steps, Allocator.Temp);

            m_Steps.AddRange(stepArray);
        }

        public PipelineResult ExecuteSteps(MessageReceiveParams<TId, TMessageMetadata> messageParams)
        {
            foreach (var step in m_Steps)
            {
                PipelineStepResult stepResult = step.Execute(messageParams);

                if(stepResult == PipelineStepResult.DisconnectClient)
                {
                    return PipelineResult.DisconnectClient;
                }

                if(stepResult != PipelineStepResult.Success)
                {
                    return PipelineResult.DiscardMessage;
                }
            }

            return PipelineResult.Success;
        }

        public PipelineResult HandleIncomingMessage(TId connectionUID, TMessageMetadata messageMetadata, ref DataStreamReader stream)
        {
            MessageReceiveParams<TId, TMessageMetadata> messageParams = new(connectionUID, messageMetadata, ref stream);

            return ExecuteSteps(messageParams);
        }
    }
}
