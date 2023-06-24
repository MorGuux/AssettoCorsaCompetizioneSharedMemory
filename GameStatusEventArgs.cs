using System;

namespace AssettoCorsaCompetizioneSharedMemory
{
    public class GameStatusEventArgs : EventArgs
    {
        public ACC_STATUS GameStatus { get; private set; }

        public GameStatusEventArgs(ACC_STATUS status)
        {
            GameStatus = status;
        }
    }
}
