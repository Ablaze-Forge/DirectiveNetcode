public enum ErrorCodes : int
{

    #region Global Errors (1 - 999)

    #endregion

    #region Engine Errors (1000 - 1020)

    #region Common Engine Error (1000 - 1010)

    Engine_UpdateTypeNotFoundOnSystem = 1000,
    Engine_Stop_NoTickDelegateFound = 1001,

    #endregion

    #region Server Engine Error (1011 - 1020)

    ServerEngine_InvalidDriverCount = 1011,
    ServerEngine_MultipleNetworkDrivers = 1012,
    ServerEngine_MaxPlayersZero = 1013,
    ServerEngine_DriverBindFailure = 1014,
    ServerEngine_Start_NoBoundDriver = 1015,

    #endregion

    #region Client Engine Error (1021 - 1030)

    #endregion

    #endregion

}

public enum WarningCodes : int
{

    #region Global Warning (1 - 999)

    Global_LoggerNotProvided = 1,

    #endregion

    #region Engine Warning (1000 - 1020)

    #region Common Engine Warning (1000 - 1010)

    Engine_CustomLoopInjected = 1000,
    Engine_Start_InvalidState = 1001,
    Engine_Stop_InvalidState = 1002,
    Engine_UpdateTypeNotFoundOnSystem = 1003,
    Engine_HardStopInProgress = 1004,
    Engine_HardStop_MissingDelegates = 1005,

    #endregion

    #region Server Engine Warning (1011 - 1020)

    ServerEngine_ConnectionDropped_MaxReached = 1011,
    ServerEngine_Disconnect_UIDNotFound = 1012,
    ServerEngine_ConnectionHandlerMissing = 1013,
    ServerEngine_Disconnect_ConnectionNotRegistered = 1014,
    ServerEngine_BindFailed = 1015,

    #endregion

    #region Client Engine Warning (1021 - 1030)

    #endregion

    #endregion

}
