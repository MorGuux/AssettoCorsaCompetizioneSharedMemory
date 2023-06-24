using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AssettoCorsaCompetizioneSharedMemory
{
    public delegate void PhysicsUpdatedHandler(object sender, PhysicsEventArgs e);
    public delegate void GraphicsUpdatedHandler(object sender, GraphicsEventArgs e);
    public delegate void StaticInfoUpdatedHandler(object sender, StaticInfoEventArgs e);
    public delegate void GameStatusChangedHandler(object sender, GameStatusEventArgs e);

    public class GameNotStartedException : Exception
    {
        public GameNotStartedException()
            : base("Shared Memory not connected, is Assetto Corsa Competizione running and have you run Connect()?")
        {
        }
    }

    enum ACC_MEMORY_STATUS { DISCONNECTED, CONNECTING, CONNECTED }

    public sealed class AssettoCorsaCompetizione
    {
        private ACC_MEMORY_STATUS _memoryStatus = ACC_MEMORY_STATUS.DISCONNECTED;
        public bool IsRunning => _memoryStatus == ACC_MEMORY_STATUS.CONNECTED;

        private readonly MemoryDataSection<Physics> _physicsData = new MemoryDataSection<Physics>("Local\\acpmf_physics");
        private readonly MemoryDataSection<Graphics> _graphicsData = new MemoryDataSection<Graphics>("Local\\acpmf_graphics");
        private readonly MemoryDataSection<StaticInfo> _staticInfoData = new MemoryDataSection<StaticInfo>("Local\\acpmf_static");

        private readonly Task _physicsPollingTask;
        private readonly CancellationTokenSource _physicsCancellationTokenSource;

        private readonly Task _graphicsPollingTask;
        private readonly CancellationTokenSource _graphicsCancellationTokenSource;

        private readonly Task _staticInfoPollingTask;
        private readonly CancellationTokenSource _staticInfoCancellationTokenSource;

        public event GameStatusChangedHandler GameStatusChanged;

        private ACC_STATUS _gameStatus = ACC_STATUS.ACC_OFF;

        internal ACC_STATUS GameStatus
        {
            get => _gameStatus;
            set
            {
                if (_gameStatus == value) return;

                _gameStatus = value;
                GameStatusChanged?.Invoke(this, new GameStatusEventArgs(_gameStatus));
            }
        }

        public AssettoCorsaCompetizione()
        {
            _physicsCancellationTokenSource = new CancellationTokenSource();
            _graphicsCancellationTokenSource = new CancellationTokenSource();
            _staticInfoCancellationTokenSource = new CancellationTokenSource();

            _physicsPollingTask = CreatePollingTask(PhysicsInterval, _physicsData, _physicsCancellationTokenSource);
            _graphicsPollingTask = CreatePollingTask(GraphicsInterval, _graphicsData, _graphicsCancellationTokenSource);
            _staticInfoPollingTask = CreatePollingTask(StaticInfoInterval, _staticInfoData, _staticInfoCancellationTokenSource);

            _physicsData.DataUpdated += _physicsData_DataUpdated;
            _graphicsData.DataUpdated += _graphicsData_DataUpdated;
            _staticInfoData.DataUpdated += _staticInfoData_DataUpdated;
        }

        private Task CreatePollingTask<T>(int delay, MemoryDataSection<T> dataSection, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken token = cancellationTokenSource.Token;
            Task listener = new Task(() =>
            {
                while (true)
                {
                    dataSection.ProcessData(_memoryStatus);

                    Thread.Sleep(delay);
                    if (token.IsCancellationRequested)
                        break;
                }
            }, token, TaskCreationOptions.LongRunning);
            return listener;
        }

        private void _staticInfoData_DataUpdated(object sender, StaticInfo e)
        {
            StaticInfoUpdated?.Invoke(this, new StaticInfoEventArgs(e));
        }

        private void _graphicsData_DataUpdated(object sender, Graphics e)
        {
            if (e.Status != GameStatus)
                GameStatus = e.Status;

            GraphicsUpdated?.Invoke(this, new GraphicsEventArgs(e));
        }

        private void _physicsData_DataUpdated(object sender, Physics e)
        {
            PhysicsUpdated?.Invoke(this, new PhysicsEventArgs(e));
        }

        /// <summary>
        /// Connect to shared memory and start polling for updates
        /// </summary>
        /// <exception cref="FileNotFoundException">The shared memory file could not be located</exception>
        public void Connect()
        {
            try
            {
                _memoryStatus = ACC_MEMORY_STATUS.CONNECTING;

                // Connect to shared memory
                _physicsData.Connect();
                _graphicsData.Connect();
                _staticInfoData.Connect();

                _physicsPollingTask.Start();
                _graphicsPollingTask.Start();
                _staticInfoPollingTask.Start();

                _memoryStatus = ACC_MEMORY_STATUS.CONNECTED;
            }
            catch (FileNotFoundException)
            {
                _physicsCancellationTokenSource.Cancel();
                _graphicsCancellationTokenSource.Cancel();
                _staticInfoCancellationTokenSource.Cancel();

                GameStatus = ACC_STATUS.ACC_OFF;

                throw new FileNotFoundException("The shared memory file could not be located");
            }
        }

        /// <summary>
        /// Stop the timers and dispose of the shared memory handles
        /// </summary>
        public void Stop()
        {
            _memoryStatus = ACC_MEMORY_STATUS.DISCONNECTED;

            // Stop the timers
            if (_physicsPollingTask.Status == TaskStatus.Running)
                _physicsCancellationTokenSource.Cancel();

            if (_graphicsPollingTask.Status == TaskStatus.Running)
                _graphicsCancellationTokenSource.Cancel();

            if (_staticInfoPollingTask.Status == TaskStatus.Running)
                _staticInfoCancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Interval for physics updates in milliseconds
        /// </summary>
        public int PhysicsInterval = 16;

        /// <summary>
        /// Interval for graphics updates in milliseconds
        /// </summary>
        public int GraphicsInterval = 100;

        /// <summary>
        /// Interval for static info updates in milliseconds
        /// </summary>
        public int StaticInfoInterval = 5000;

        /// <summary>
        /// Represents the method that will handle the physics update events
        /// </summary>
        public event PhysicsUpdatedHandler PhysicsUpdated;

        /// <summary>
        /// Represents the method that will handle the graphics update events
        /// </summary>
        public event GraphicsUpdatedHandler GraphicsUpdated;

        /// <summary>
        /// Represents the method that will handle the static info update events
        /// </summary>
        public event StaticInfoUpdatedHandler StaticInfoUpdated;

        /// <summary>
        /// Read the physics data from shared memory. This is a one-time read and will not update until called again.
        /// </summary>
        /// <returns>Physics data</returns>
        public Physics ReadPhysics()
        {
            return _physicsData.ReadMemory();
        }

        /// <summary>
        /// Read the graphics data from shared memory. This is a one-time read and will not update until called again.
        /// </summary>
        /// <returns>Graphics data</returns>
        public Graphics ReadGraphics()
        {
            return _graphicsData.ReadMemory();
        }

        /// <summary>
        /// Read the static info data from shared memory. This is a one-time read and will not update until called again.
        /// </summary>
        /// <returns>Static info data</returns>
        public StaticInfo ReadStaticInfo()
        {
            return _staticInfoData.ReadMemory();
        }
    }
}
