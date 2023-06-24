using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace AssettoCorsaCompetizioneSharedMemory
{
    internal class MemoryDataSection<T>
    {
        private MemoryMappedFile _memoryMappedFile;
        private readonly string _mapName;

        internal delegate void DataUpdatedHandler(object sender, T e);
        /// <summary>
        /// Represents the method that will handle the data update event
        /// </summary>
        internal event DataUpdatedHandler DataUpdated;

        internal MemoryDataSection(string mapName)
        {
            this._mapName = mapName;
        }

        internal void Connect()
        {
            try
            {
                _memoryMappedFile = MemoryMappedFile.OpenExisting(_mapName);
            }
            catch (FileNotFoundException)
            {
                _memoryMappedFile = null;
                throw new GameNotStartedException();
            }
        }

        /// <summary>
        /// Read the current T struct's data from shared memory
        /// </summary>
        /// <returns>A populated struct representing the current data stored in the memory mapped file, or null if not available</returns>
        internal T ReadMemory()
        {
            if (_memoryMappedFile == null)
                throw new GameNotStartedException();

            using (var stream = _memoryMappedFile.CreateViewStream())
            {
                using (var reader = new BinaryReader(stream))
                {
                    var size = Marshal.SizeOf(typeof(T));
                    var bytes = reader.ReadBytes(size);
                    var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
                    var data = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                    handle.Free();
                    return data;
                }
            }
        }

        internal void ProcessData(ACC_MEMORY_STATUS status)
        {
            if (status == ACC_MEMORY_STATUS.DISCONNECTED)
                return;

            T data = ReadMemory();
            DataUpdated?.Invoke(this, data);
        }
    }
}
