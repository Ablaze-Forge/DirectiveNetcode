using AblazeForge.DirectiveNetcode.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace AblazeForge.DirectiveNetcode.Engines
{
    public abstract class TickSystem
    {
        #region Setup

        protected IDirectiveNetcodeLogger Logger { get; }

        private readonly Type m_UpdateType;

        protected TickSystem(IDirectiveNetcodeLogger logger, Type updateType = null)
        {
            Logger = logger;
            m_UpdateType = updateType ?? typeof(FixedUpdate);

            CheckUpdateType();
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        private void CheckUpdateType()
        {
            if (!(m_UpdateType == typeof(FixedUpdate) ||
                m_UpdateType == typeof(Update) ||
                m_UpdateType == typeof(EarlyUpdate) ||
                m_UpdateType == typeof(PreLateUpdate) ||
                m_UpdateType == typeof(PostLateUpdate)))
            {
                Logger.LogWarning(null, GetType().Name, WarningCodes.TickSystem_CustomLoopInjected, "Please be careful when using custom Update types. Also ensure the Type being used is available at all times inside the PlayerLoopSystem. [This message only appears on Debug and Development builds.]");
            }
        }

        #endregion

        private PlayerLoopSystem m_CustomTickSystem;

        public TickSystemState State => m_State;
        private TickSystemState m_State = TickSystemState.Stopped;

        protected abstract void Tick();

        protected bool StartTicking()
        {
            if (m_State != TickSystemState.Stopped)
            {
                Logger.Log(GetType().Name, $"Tick system {GetType().Name} already started or in the process of starting.");
                return false;
            }

            m_State = TickSystemState.Starting;


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
                Logger.LogError(GetType().Name, ErrorCodes.TickSystem_UpdateTypeNotFoundOnSystem, $"Could not find the {m_UpdateType.Name} subsystem in the PlayerLoop. Tick system cannot start.");
                m_State = TickSystemState.Stopped;
                return false;
            }

            m_CustomTickSystem = new PlayerLoopSystem
            {
                type = typeof(CustomTickSystemCategory),
                updateDelegate = Tick,
            };

            List<PlayerLoopSystem> targetSubsystems = new(currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList)
            {
                m_CustomTickSystem
            };

            currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList = targetSubsystems.ToArray();

            PlayerLoop.SetPlayerLoop(currentPlayerLoop);

            m_State = TickSystemState.Running;
            Logger.Log(GetType().Name, $"Tick system for {GetType().Name} started.");

            return true;
        }

        protected bool StopTicking()
        {
            if (m_State != TickSystemState.Running)
            {
                Logger.LogWarning(GetType().Name, WarningCodes.TickSystem_Stop_InvalidState, "Tick system is not currently running.");
                return false;
            }

            m_State = TickSystemState.Stopping;

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
                Logger.LogWarning(GetType().Name, WarningCodes.TickSystem_UpdateTypeNotFoundOnSystem, $"Could not find the {m_UpdateType.Name} subsystem in the PlayerLoop to stop ticking. Don't remove custom Update types from the PlayerLoop that contains the injected Tick method.");
                m_State = TickSystemState.Stopped;
                return false;
            }

            List<PlayerLoopSystem> targetSubsystems = new(currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList);

            int removedCount = targetSubsystems.RemoveAll(sys =>
                sys.type == typeof(CustomTickSystemCategory) &&
                sys.updateDelegate?.Target == this);

            if (removedCount > 0)
            {
                currentPlayerLoop.subSystemList[targetSubsystemIndex].subSystemList = targetSubsystems.ToArray();
                PlayerLoop.SetPlayerLoop(currentPlayerLoop);
                m_State = TickSystemState.Stopped;
                Logger.Log(GetType().Name, $"Tick system {GetType().Name} stopped ticking. Removed {removedCount} custom system(s) from {m_UpdateType.Name} loop.");
                return true;
            }
            else
            {
                Logger.LogError(GetType().Name, ErrorCodes.TickSystem_Stop_NoTickDelegateFound, $"Could not find *this instance's* custom tick system for {GetType().Name} to remove from {m_UpdateType.Name} loop. This indicates an unexpected external modification or corruption of the PlayerLoop. This tick system instance is now in an unrecoverable state. Use ForceStop() to attempt a cleanup and reset if needed.");
                m_State = TickSystemState.Unrecoverable;
                return false;
            }
        }

        public void HardStop()
        {
            Logger.LogWarning(GetType().Name, WarningCodes.TickSystem_HardStopInProgress, "Attempting a hard stop to clean all instance-specific methods from the PlayerLoop.");

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
                            Logger.Log(GetType().Name, $"Removed instance's Tick method from PlayerLoop subsystem type: {currentTopLevelSystem.type.Name}");
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
                Logger.Log(GetType().Name, $"Hard stopped tick system. Removed {removedCount} instance-specific method(s) from the PlayerLoop.");
            }
            else
            {
                Logger.LogWarning(GetType().Name, WarningCodes.TickSystem_HardStop_MissingDelegates, "No instance-specific methods were found in the PlayerLoop to hard stop.");
            }

            m_State = TickSystemState.Stopped;
        }

        public enum TickSystemState
        {
            Stopped,
            Starting,
            Running,
            Stopping,
            Unrecoverable,
        }

        private readonly struct CustomTickSystemCategory { }
    }
}
