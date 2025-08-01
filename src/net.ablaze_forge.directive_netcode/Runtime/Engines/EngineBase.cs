using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using AblazeForge.DirectiveNetcode.Logging;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public abstract class EngineBase
    {
        protected ErrorCodeLoggerBase Logger { get; private set; }

        private readonly Type m_UpdateType;

        /// <summary>
        /// Initializes a new instance of the EngineBase class.
        /// </summary>
        /// <param name="logger">The logger instance for this engine.</param>
        /// <param name="updateType">
        /// The type of PlayerLoopSystem to inject into (e.g., typeof(FixedUpdate), typeof(Update)).
        /// Defaults to typeof(FixedUpdate) if null.
        /// </param>
        protected EngineBase(ErrorCodeLoggerBase logger, Type updateType = null)
        {
            Logger = logger;

            if (Logger == null)
            {
                Logger = new ErrorCodeLogger(Debug.unityLogger);
                Logger.LogWarning(this, WarningCodes.Global_LoggerNotProvided, "ErrorCodeLoggerBase was null. Defaulting to ErrorCodeLogger with UnityLogger.");
            }

            m_UpdateType = updateType ?? typeof(FixedUpdate);

            if (!(m_UpdateType == typeof(FixedUpdate) ||
                m_UpdateType == typeof(Update) ||
                m_UpdateType == typeof(EarlyUpdate) ||
                m_UpdateType == typeof(PreLateUpdate) ||
                m_UpdateType == typeof(PostLateUpdate)))
            {
                Logger.LogWarning(this, WarningCodes.Engine_CustomLoopInjected, "Please be careful when using custom Update types. Also ensure the Type being used is available at all times inside the PlayerLoopSystem.");
            }
        }

        private PlayerLoopSystem m_CustomTickSystem;

        public EngineState State => m_State;
        private EngineState m_State = EngineState.Stopped;

        protected abstract void Tick();

        /// <summary>
        /// Injects the Tick method into the PlayerLoopSystem
        /// </summary>
        /// <returns>Returns <c>true</c> if the Tick method was correctly injected in the system</returns>
        /// <remarks>
        /// This method will log errors and return <c>false</c> if:
        /// <list type="bullet">
        ///     <item><description>The engine is not stopped</description></item>
        ///     <item><description>A invalid Update Type was provided and the PlayerLoopSystem was not found</description></item>
        /// </list>
        /// </remarks>
        protected bool StartTicking()
        {
            if (m_State != EngineState.Stopped)
            {
                Logger.LogWarning(this, WarningCodes.Engine_Start_InvalidState, "Engine is not stopped. Call StopTicking() and ensure it's not in a Unrecoverable state first if you want to restart.");
                return false;
            }

            m_State = EngineState.Starting;

            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            int targetSubsystemIndex = -1;

            for (int i = 0; i < currentPlayerLoop.subSystemList.Length; i++)
            {

                if (currentPlayerLoop.subSystemList[i].type == m_UpdateType)
                {
                    targetSubsystemIndex = i;
                    break;
                }
            }

            if (targetSubsystemIndex == -1)
            {
                Logger.LogError(this, ErrorCodes.Engine_UpdateTypeNotFoundOnSystem, $"Could not find the {m_UpdateType.Name} subsystem in the PlayerLoop. Engine cannot start ticking.");
                m_State = EngineState.Stopped;
                return false;
            }

            m_CustomTickSystem = new PlayerLoopSystem
            {
                type = typeof(CustomEngineTickCategory),
                updateDelegate = Tick,
            };

            List<PlayerLoopSystem> targetSubsystems = new(currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList)
            {
                m_CustomTickSystem
            };

            currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList = targetSubsystems.ToArray();

            PlayerLoop.SetPlayerLoop(currentPlayerLoop);

            m_State = EngineState.Running;
            Logger.Log(this, $"Engine {GetType().Name} started ticking.");

            return true;
        }

        /// <summary>
        /// Removes the injected Tick method from the PlayerLoopSystem
        /// </summary>
        /// <returns>Returns <c>true</c> if the Tick method was found and removed successfully from the system</returns>
        /// <remarks>
        /// This method will log errors and return <c>false</c> if:
        /// <list type="bullet">
        ///     <item><description>The Engine is not running</description></item>
        ///     <item><description>The PlayerLoopSystem was not found</description></item>
        ///     <item><description>The Tick method was not found registered under the instance calling this method</description></item>
        /// </list>
        /// </remarks>
        protected bool StopTicking()
        {
            if (m_State != EngineState.Running)
            {
                Logger.LogWarning(this, WarningCodes.Engine_Stop_InvalidState, "Engine is not currently running.");
                return false;
            }

            m_State = EngineState.Stopping;

            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            int targetSubsystemIndex = -1;

            for (int i = 0; i < currentPlayerLoop.subSystemList.Length; i++)
            {
                if (currentPlayerLoop.subSystemList[i].type == m_UpdateType)
                {
                    targetSubsystemIndex = i;
                    break;
                }
            }

            if (targetSubsystemIndex == -1)
            {
                Logger.LogWarning(this, WarningCodes.Engine_UpdateTypeNotFoundOnSystem, $"Could not find the {m_UpdateType.Name} subsystem in the PlayerLoop to stop ticking. Don't remove custom Update types from the PlayerLoop that contains the injected Tick method.");
                m_State = EngineState.Stopped;
                return false;
            }

            List<PlayerLoopSystem> targetSubsystems = new(currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList);

            int removedCount = targetSubsystems.RemoveAll(sys =>
                sys.type == typeof(CustomEngineTickCategory) &&
                sys.updateDelegate?.Target == this);

            if (removedCount > 0)
            {
                currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList = targetSubsystems.ToArray();
                PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                m_State = EngineState.Stopped;
                Logger.Log(this, $"Engine {GetType().Name} stopped ticking. Removed {removedCount} custom system(s) from {m_UpdateType.Name} loop.");
                return true;
            }
            else
            {
                Logger.LogError(this, ErrorCodes.Engine_Stop_NoTickDelegateFound, $"Could not find *this instance's* custom tick system for {GetType().Name} to remove from {m_UpdateType.Name} loop. This indicates an unexpected external modification or corruption of the PlayerLoop. This engine instance is now in an unrecoverable state. Use ForceStop() to attempt a cleanup and reset if needed.");
                m_State = EngineState.Unrecoverable;
                return false;
            }
        }

        /// <summary>
        /// Forces the removal of all delegate methods associated with THIS engine instance registered in ANY PlayerLoopSystem subsystem.
        /// </summary>
        /// <remarks>
        /// This method is intended as a thorough cleanup, ensuring that any instances of this engine's <see cref="Tick"/> method are removed from the PlayerLoop, even if they were manually placed in unexpected subsystems.
        ///
        /// Performance is slow due to the need to run on all PlayerLoopSystems, use only when the Engine reaches a <see cref="EngineState.Unrecoverable"/> state.
        ///
        /// It operates regardless of the engine's current state and will reset the engine's state to <see cref="EngineState.Stopped"/> upon completion, allowing it to potentially be restarted by <see cref="StartTicking"/> again.
        /// </remarks>
        public void HardStop()
        {
            Logger.LogWarning(this, WarningCodes.Engine_HardStopInProgress, "Attempting a hard stop to clean all instance-specific methods from the PlayerLoop.");

            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            int removedCount = 0;

            List<PlayerLoopSystem> newPlayerLoopList = new(currentPlayerLoop.subSystemList.Length);

            for (int i = 0; i < currentPlayerLoop.subSystemList.Length; i++)
            {
                PlayerLoopSystem currentTopLevelSystem = currentPlayerLoop.subSystemList[i];
                List<PlayerLoopSystem> newSubsystemList = new();

                if (currentTopLevelSystem.subSystemList != null)
                {
                    foreach (PlayerLoopSystem subSystem in currentTopLevelSystem.subSystemList)
                    {
                        if (subSystem.updateDelegate?.Target == this)
                        {
                            removedCount++;
                            Logger.Log(this, $"Removed instance's Tick method from PlayerLoop subsystem type: {currentTopLevelSystem.type.Name}");
                        }
                        else
                        {
                            newSubsystemList.Add(subSystem);
                        }
                    }
                }

                currentTopLevelSystem.subSystemList = newSubsystemList.ToArray();
                newPlayerLoopList.Add(currentTopLevelSystem);
            }

            currentPlayerLoop.subSystemList = newPlayerLoopList.ToArray();
            PlayerLoop.SetPlayerLoop(currentPlayerLoop);

            if (removedCount > 0)
            {
                Logger.Log(this, $"Hard stopped engine. Removed {removedCount} instance-specific method(s) from the PlayerLoop.");
            }
            else
            {
                Logger.LogWarning(this, WarningCodes.Engine_HardStop_MissingDelegates, "No instance-specific methods were found in the PlayerLoop to hard stop.");
            }

            m_State = EngineState.Stopped;
        }

        private struct CustomEngineTickCategory { }

        public enum EngineState
        {
            Stopped,
            Starting,
            Running,
            Stopping,
            Unrecoverable,
        }
    }
}
