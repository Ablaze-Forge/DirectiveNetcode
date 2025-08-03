/// <summary>
/// Defines error codes used throughout the DirectiveNetcode package for structured error reporting.
/// Error codes are organized by component and function to facilitate debugging and error tracking.
/// </summary>
public enum ErrorCodes : int
{
    #region Global Errors (1 - 999)

    #endregion

    #region Engine Errors (1000 - 1020)

    #region Common Engine Error (1000 - 1010)

    /// <summary>
    /// Indicates that the specified PlayerLoopSystem update type was not found when attempting to inject the engine's tick method.
    /// </summary>
    Engine_UpdateTypeNotFoundOnSystem = 1000,

    /// <summary>
    /// Indicates that the engine's tick delegate could not be found when attempting to stop the engine.
    /// </summary>
    Engine_Stop_NoTickDelegateFound = 1001,

    #endregion

    #region Server Engine Error (1011 - 1020)

    /// <summary>
    /// Indicates that an invalid number of network driver configurations were provided when starting the server.
    /// </summary>
    ServerEngine_InvalidDriverCount = 1011,

    /// <summary>
    /// Indicates that an attempt was made to start the server engine when a MultiNetworkDriver instance already exists.
    /// </summary>
    ServerEngine_MultipleNetworkDrivers = 1012,

    /// <summary>
    /// Indicates that the maximum number of players was set to zero when starting the server.
    /// </summary>
    ServerEngine_MaxPlayersZero = 1013,

    /// <summary>
    /// Indicates that a network driver failed to bind to its endpoint and stopOnFailure was set to true.
    /// </summary>
    ServerEngine_DriverBindFailure = 1014,

    /// <summary>
    /// Indicates that no network drivers were successfully bound when starting the server.
    /// </summary>
    ServerEngine_Start_NoBoundDriver = 1015,

    #endregion

    #region Client Engine Error (1021 - 1030)

    #endregion

    #endregion
}

/// <summary>
/// Defines warning codes used throughout the DirectiveNetcode library for structured warning reporting.
/// Warning codes are organized by component and function to facilitate debugging and issue tracking.
/// </summary>
public enum WarningCodes : int
{
    #region Global Warning (1 - 999)

    /// <summary>
    /// Indicates that no logger was provided to the engine, and a default Unity logger is being used instead.
    /// </summary>
    Global_LoggerNotProvided = 1,

    #endregion

    #region Engine Warning (1000 - 1020)

    #region Common Engine Warning (1000 - 1010)

    /// <summary>
    /// Indicates that a custom PlayerLoopSystem update type was injected, which requires careful handling.
    /// </summary>
    Engine_CustomLoopInjected = 1000,

    /// <summary>
    /// Indicates that the engine is not in a stopped state when attempting to start it.
    /// </summary>
    Engine_Start_InvalidState = 1001,

    /// <summary>
    /// Indicates that the engine is not in a running state when attempting to stop it.
    /// </summary>
    Engine_Stop_InvalidState = 1002,

    /// <summary>
    /// Indicates that the specified PlayerLoopSystem update type was not found when attempting to stop the engine.
    /// </summary>
    Engine_UpdateTypeNotFoundOnSystem = 1003,

    /// <summary>
    /// Indicates that a hard stop operation is in progress to clean up instance-specific methods from the PlayerLoop.
    /// </summary>
    Engine_HardStopInProgress = 1004,

    /// <summary>
    /// Indicates that no instance-specific methods were found in the PlayerLoop during a hard stop operation.
    /// </summary>
    Engine_HardStop_MissingDelegates = 1005,

    #endregion

    #region Server Engine Warning (1011 - 1020)

    /// <summary>
    /// Indicates that a new connection was dropped because the maximum connection count was reached.
    /// </summary>
    ServerEngine_ConnectionDropped_MaxReached = 1011,

    /// <summary>
    /// Indicates that a disconnect was attempted with an invalid UID (less than 1).
    /// </summary>
    ServerEngine_Disconnect_UIDNotFound = 1012,

    /// <summary>
    /// Indicates that a NetworkConnectionHandler could not be found in the active connections list during disconnect.
    /// </summary>
    ServerEngine_ConnectionHandlerMissing = 1013,

    /// <summary>
    /// Indicates that a disconnect was attempted with an unregistered UID.
    /// </summary>
    ServerEngine_Disconnect_ConnectionNotRegistered = 1014,

    /// <summary>
    /// Indicates that a network driver failed to bind to its endpoint.
    /// </summary>
    ServerEngine_BindFailed = 1015,

    #endregion

    #region Client Engine Warning (1021 - 1030)

    #endregion

    #endregion
}
