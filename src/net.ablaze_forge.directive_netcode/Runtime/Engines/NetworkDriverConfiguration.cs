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
        /// Provides a default configuration for a UDP-based <see cref="NetworkDriver"/>.
        /// </summary>
        public static NetworkDriverConfiguration<UDPNetworkInterface> UdpConfiguration => new();

        /// <summary>
        /// Provides a default configuration for a WebSocket-based <see cref="NetworkDriver"/>.
        /// </summary>
        public static NetworkDriverConfiguration<WebSocketNetworkInterface> WebSocketConfiguration => new(port: 7778);

        /// <summary>
        /// Specifies whether to use IPv4. If false, IPv6 will be used.
        /// </summary>
        public bool UseIPv4 { get; private set; }

        /// <summary>
        /// The network port to be used by the driver.
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkDriverConfiguration"/> class.
        /// </summary>
        /// <param name="port">The network port for the driver.</param>
        /// <param name="useIPv4">True to use IPv4 and false to use IPv6.</param>
        protected NetworkDriverConfiguration(ushort port, bool useIPv4)
        {
            Port = port;
            UseIPv4 = useIPv4;
        }

        /// <summary>
        /// Creates and returns a configured <see cref="NetworkDriver"/> instance based on this configuration.
        /// </summary>
        /// <returns>A new <see cref="NetworkDriver"/>.</returns>
        public abstract NetworkDriver GetNetworkDriver();
    }

    /// <summary>
    /// Configures a <see cref="NetworkDriver"/> for a specific <see cref="INetworkInterface"/> type.
    /// </summary>
    /// <remarks>
    /// This generic class allows specifying the underlying network protocol (e.g., UDP, WebSocket, or a custom implementation) at compile time.
    /// </remarks>
    /// <typeparam name="T"> The struct type that implements <see cref="INetworkInterface"/>, defining the low-level network communication protocol. Must be an unmanaged struct. </typeparam>
    public class NetworkDriverConfiguration<T> : NetworkDriverConfiguration where T : unmanaged, INetworkInterface
    {
        /// <summary>
        /// The instance of the <see cref="INetworkInterface"/> struct used by this configuration.
        /// </summary>
        public T NetworkInterfaceInstance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkDriverConfiguration{T}"/> class.
        /// The <typeparamref name="T"/> network interface instance will be default-constructed.
        /// </summary>
        /// <param name="port">The network port for the driver. Defaults to 7777.</param>
        /// <param name="useIPv4">True to use IPv4 and false to use IPv6. Defaults to <c>true</c>.</param>
        public NetworkDriverConfiguration(ushort port = 7777, bool useIPv4 = true) : base(port, useIPv4)
        {
            NetworkInterfaceInstance = new T();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkDriverConfiguration{T}"/> class with a pre-existing <see cref="INetworkInterface"/> instance.
        /// </summary>
        /// <remarks>
        /// Use this constructor when your custom <see cref="INetworkInterface"/> implementation requires specific initialization parameters that cannot be provided by a default constructor.
        /// </remarks>
        /// <param name="networkInterfaceInstance">The pre-configured <see cref="INetworkInterface"/> instance.</param>
        /// <param name="port">The network port for the driver. Defaults to 7777.</param>
        /// <param name="useIPv4">True to use IPv4 and false to use IPv6. Defaults to <c>true</c>.</param>
        public NetworkDriverConfiguration(T networkInterfaceInstance, ushort port = 7777, bool useIPv4 = true) : base(port, useIPv4)
        {
            NetworkInterfaceInstance = networkInterfaceInstance;
        }

        /// <summary>
        /// Overrides the base method to create a <see cref="NetworkDriver"/> using the specific <see cref="INetworkInterface"/> defined by this configuration.
        /// </summary>
        /// <returns>A new <see cref="NetworkDriver"/> instance.</returns>
        public override NetworkDriver GetNetworkDriver()
        {
            return NetworkDriver.Create(NetworkInterfaceInstance);
        }
    }
}
