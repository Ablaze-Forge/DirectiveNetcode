using System;
using System.Collections.Generic;
using Unity.Networking.Transport;

namespace AblazeForge.DirectiveNetcode.Engines
{
    /// <summary>
    /// Abstract base class for configuring a <see cref="NetworkDriver"/>.
    /// Provides common network settings and factory methods for standard driver types.
    /// </summary>
    /// <remarks>
    /// Derive from this class to create specific configurations for different <see cref="INetworkInterface"/> implementations (e.g., UDP, WebSocket, or custom).
    /// </remarks>
    public abstract class NetworkDriverConfiguration
    {
        /// <summary>
        /// Provides a default configuration for a **UDP**-based <see cref="NetworkDriver"/> (<seealso cref="NetworkDriverConfiguration{UDPNetworkInterface}"/>).
        /// </summary>
        public static NetworkDriverConfiguration UdpConfiguration => new NetworkDriverConfiguration<UDPNetworkInterface>();

        /// <summary>
        /// Provides a default configuration for a **IPC**-based <see cref="NetworkDriver"/> (<seealso cref="NetworkDriverConfiguration{IPCNetworkInterface}"/>).
        /// </summary>
        public static NetworkDriverConfiguration IpcConfiguration => new NetworkDriverConfiguration<IPCNetworkInterface>();

        /// <summary>
        /// Provides a default configuration for a **WebSocket**-based <see cref="NetworkDriver"/> (<seealso cref="NetworkDriverConfiguration{WebSocketNetworkInterface}"/>).
        /// </summary>
        /// <remarks>
        /// This configuration explicitly sets the reliable pipeline to <see cref="PipelineStageConfiguration.UnreliableDefaultConfiguration"/> because WebSocket runs over TCP, which already provides reliable delivery. 
        /// Adding an additional reliable stage would incur unnecessary overhead.
        /// </remarks>
        public static NetworkDriverConfiguration WebSocketConfiguration => new NetworkDriverConfiguration<WebSocketNetworkInterface>()
            .WithFragmentationPipelineStage(new PipelineStageConfiguration(typeof(FragmentationPipelineStage)))
            .WithReliablePipelineStage(PipelineStageConfiguration.UnreliableDefaultConfiguration);

        /// <summary>
        /// Specifies whether to use IPv4. If false, IPv6 will be used.
        /// </summary>
        public bool IsIPv4 { get; protected set; } = true;

        /// <summary>
        /// The network port to be used by the driver. Defaults to 7777.
        /// </summary>
        public ushort Port { get; protected set; } = 7777;

        /// <summary>
        /// The struct containing the settings for the driver.
        /// </summary>
        public NetworkSettings NetworkSettings { get; protected set; } = new NetworkSettings();

        /// <summary>
        /// The configuration for the unreliable pipeline (default: <see cref="NullPipelineStage"/>).
        /// </summary>
        public PipelineStageConfiguration UnreliablePipelineIds { get; protected set; } = PipelineStageConfiguration.UnreliableDefaultConfiguration;

        /// <summary>
        /// The configuration for the reliable pipeline (default: <see cref="ReliableSequencedPipelineStage"/>).
        /// </summary>
        public PipelineStageConfiguration ReliablePipelineIds { get; protected set; } = PipelineStageConfiguration.ReliableSequencedDefaultConfiguration;

        /// <summary>
        /// The configuration for the unreliable sequenced pipeline (default: <see cref="UnreliableSequencedPipelineStage"/>).
        /// </summary>
        public PipelineStageConfiguration UnreliableSequencedPipelineIds { get; protected set; } = PipelineStageConfiguration.UnreliableSequencedDefaultConfiguration;

        /// <summary>
        /// The configuration for the fragmentation pipeline (default: <see cref="FragmentationPipelineStage"/>).
        /// </summary>
        public PipelineStageConfiguration FragmentationPipelineIds { get; protected set; } = PipelineStageConfiguration.FragmentedDefaultConfiguration;

        /// <summary>
        /// Configures the driver to use IPv4.
        /// </summary>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration UseIpV4()
        {
            IsIPv4 = true;

            return this;
        }

        /// <summary>
        /// Configures the driver to use IPv6.
        /// </summary>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration UseIpv6()
        {
            IsIPv4 = false;

            return this;
        }

        /// <summary>
        /// Sets the network port for the driver.
        /// </summary>
        /// <param name="port">The port number to use.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration WithPort(ushort port)
        {
            Port = port;

            return this;
        }

        public NetworkDriverConfiguration WithSettings(NetworkSettings settings)
        {
            NetworkSettings = settings;

            return this;
        }

        /// <summary>
        /// Sets the entire pipeline configuration for unreliable messaging.
        /// </summary>
        /// <param name="pipelineStage">The pipeline configuration to use.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration WithUnreliablePipelineStage(PipelineStageConfiguration pipelineStage)
        {
            UnreliablePipelineIds = pipelineStage;

            return this;
        }

        /// <summary>
        /// Adds a single step (stage) to the unreliable pipeline.
        /// </summary>
        /// <param name="step">The type of the pipeline stage to add.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration AddUnreliablePipelineStep(Type step)
        {
            UnreliablePipelineIds.AddStep(step);

            return this;
        }

        /// <summary>
        /// Sets the entire pipeline configuration for reliable messaging.
        /// </summary>
        /// <param name="pipelineStage">The pipeline configuration to use.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration WithReliablePipelineStage(PipelineStageConfiguration pipelineStage)
        {
            ReliablePipelineIds = pipelineStage;

            return this;
        }

        /// <summary>
        /// Adds a single step (stage) to the reliable pipeline.
        /// </summary>
        /// <param name="step">The type of the pipeline stage to add.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration AddReliablePipelineStep(Type step)
        {
            ReliablePipelineIds.AddStep(step);

            return this;
        }

        /// <summary>
        /// Sets the entire pipeline configuration for unreliable sequenced messaging.
        /// </summary>
        /// <param name="pipelineStage">The pipeline configuration to use.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration WithUnreliableSequencedPipelineStage(PipelineStageConfiguration pipelineStage)
        {
            UnreliableSequencedPipelineIds = pipelineStage;

            return this;
        }

        /// <summary>
        /// Adds a single step (stage) to the unreliable sequenced pipeline.
        /// </summary>
        /// <param name="step">The type of the pipeline stage to add.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration AddUnreliableSequencedPipelineStep(Type step)
        {
            UnreliableSequencedPipelineIds.AddStep(step);

            return this;
        }

        /// <summary>
        /// Sets the entire pipeline configuration for message fragmentation.
        /// </summary>
        /// <param name="pipelineStage">The pipeline configuration to use.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration WithFragmentationPipelineStage(PipelineStageConfiguration pipelineStage)
        {
            FragmentationPipelineIds = pipelineStage;

            return this;
        }

        /// <summary>
        /// Adds a single step (stage) to the fragmentation pipeline.
        /// </summary>
        /// <param name="step">The type of the pipeline stage to add.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public NetworkDriverConfiguration AddFragmentationPipelineStep(Type step)
        {
            FragmentationPipelineIds.AddStep(step);

            return this;
        }

        /// <summary>
        /// Creates and returns a configured <see cref="NetworkDriver"/> instance based on this configuration.
        /// </summary>
        /// <returns>A new <see cref="NetworkDriver"/>.</returns>
        public abstract NetworkDriver GetNetworkDriver();

        /// <summary>
        /// Creates a new configuration instance with a different underlying <see cref="INetworkInterface"/>.
        /// </summary>
        /// <typeparam name="TInterface">The new <see cref="INetworkInterface"/> type.</typeparam>
        /// <returns>A new <see cref="NetworkDriverConfiguration"/> instance with the specified interface.</returns>
        public abstract NetworkDriverConfiguration ChangeInterface<TInterface>() where TInterface : unmanaged, INetworkInterface;
    }

    /// <summary>
    /// Configures a <see cref="NetworkDriver"/> for a specific <see cref="INetworkInterface"/> type.
    /// </summary>
    /// <remarks>
    /// This generic class allows specifying the underlying network protocol (e.g., UDP, WebSocket, or a custom implementation) at compile time.
    /// </remarks>
    /// <typeparam name="TInterface"> The struct type that implements <see cref="INetworkInterface"/>, defining the low-level network communication protocol. Must be an unmanaged struct. </typeparam>
    public class NetworkDriverConfiguration<TInterface> : NetworkDriverConfiguration where TInterface : unmanaged, INetworkInterface
    {
        /// <summary>
        /// The instance of the <see cref="INetworkInterface"/> struct used by this configuration.
        /// </summary>
        public TInterface NetworkInterfaceInstance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkDriverConfiguration{TInterface}"/> class.
        /// The <typeparamref name="TInterface"/> network interface instance will be default-constructed.
        /// </summary>
        public NetworkDriverConfiguration()
            : base()
        {
            NetworkInterfaceInstance = new TInterface();
        }

        /// <summary>
        /// Creates a new <see cref="NetworkDriverConfiguration{TNewInterface}"/> instance, preserving the current configuration's settings but swapping the network interface.
        /// </summary>
        /// <typeparam name="TNewInterface">The new <see cref="INetworkInterface"/> type.</typeparam>
        /// <returns>A new <see cref="NetworkDriverConfiguration{TNewInterface}"/> instance.</returns>
        /// <remarks>
        /// It's important to note that if the new interface is of type <see cref="WebSocketNetworkInterface"/>, users should explicitly remove reliable stages from the pipelines.
        /// That can be achieved by calling <c>.WithReliablePipelineStage(PipelineStageConfiguration.UnreliableDefaultConfiguration)</c>) to avoid redundant overhead on the TCP-based protocol.
        /// </remarks>
        public override NetworkDriverConfiguration ChangeInterface<TNewInterface>()
        {
            return new NetworkDriverConfiguration<TNewInterface>()
                .WithUnreliablePipelineStage(UnreliablePipelineIds)
                .WithReliablePipelineStage(ReliablePipelineIds)
                .WithUnreliableSequencedPipelineStage(UnreliableSequencedPipelineIds)
                .WithFragmentationPipelineStage(FragmentationPipelineIds)
                .WithPort(Port);
        }

        /// <summary>
        /// Creates and returns a configured <see cref="NetworkDriver"/> instance using the specific <typeparamref name="TInterface"/>.
        /// </summary>
        /// <returns>A new <see cref="NetworkDriver"/>.</returns>
        public override NetworkDriver GetNetworkDriver()
        {
            return NetworkDriver.Create(NetworkInterfaceInstance, NetworkSettings);
        }
    }

    /// <summary>
    /// Represents the configuration and ordering of pipeline stages used by a <see cref="NetworkDriver"/>.
    /// </summary>
    public class PipelineStageConfiguration
    {
        /// <summary>
        /// A default configuration for a reliable and sequenced pipeline stage.
        /// </summary>
        public static PipelineStageConfiguration ReliableSequencedDefaultConfiguration => new(typeof(ReliableSequencedPipelineStage));

        /// <summary>
        /// A default configuration for an unreliable but sequenced pipeline stage.
        /// </summary>
        public static PipelineStageConfiguration UnreliableSequencedDefaultConfiguration => new(typeof(UnreliableSequencedPipelineStage));

        /// <summary>
        /// A default configuration for a completely unreliable pipeline, using the <see cref="NullPipelineStage"/>.
        /// </summary>
        public static PipelineStageConfiguration UnreliableDefaultConfiguration => new(typeof(NullPipelineStage));

        /// <summary>
        /// A default configuration for a fragmented pipeline, including the <see cref="ReliableSequencedPipelineStage"/> for transport.
        /// </summary>
        public static PipelineStageConfiguration FragmentedDefaultConfiguration => new(typeof(FragmentationPipelineStage), typeof(ReliableSequencedPipelineStage));

        /// <summary>
        /// Gets an array of <see cref="Type"/> objects representing the configured pipeline stages.
        /// A new array is returned on each access to maintain state immutability.
        /// </summary>
        public Type[] Stages => m_Stages.ToArray();

        private readonly List<Type> m_Stages;

        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineStageConfiguration"/> class with the specified stages.
        /// </summary>
        /// <param name="stages">An array of stage types.</param>
        public PipelineStageConfiguration(params Type[] stages)
        {
            m_Stages = new(stages);
        }

        /// <summary>
        /// Adds a new step (stage) to the end of the pipeline.
        /// </summary>
        /// <param name="newStep">The type of the pipeline stage to add.</param>
        /// <returns>The current configuration instance for fluent chaining.</returns>
        public PipelineStageConfiguration AddStep(Type newStep)
        {
            m_Stages.Add(newStep);

            return this;
        }
    }
}
