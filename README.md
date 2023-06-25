Assetto Corsa Competizione Shared Memory Library
===================================

This library is based on the prior work of [ProBun/ACCSharedMemory](https://github.com/ProBun/ACCSharedMemory) who ported the original Assetto Corsa shared memory library [mdjarv/assettocorsasharedmemory](https://github.com/mdjarv/assettocorsasharedmemory) to work with ACC.

Maff updated it to ACC 1.8.x from the documentation supplied [here](https://www.assettocorsa.net/forum/index.php?threads/acc-shared-memory-documentation.59965/)

The code was built around the shared memory structures described [here](http://www.assettocorsa.net/forum/index.php?threads/shared-memory-reference.3352/) on the official Assetto Corsa forum

## Improvements

I have changed the following features of the library to enhance performance and readability:
* Utilising `Tasks` and `Thread.Sleep(...)` instead of Timers (each memory pipeline is handled by a separate Task for concurrency.
* Merged duplicated memory-mapped file access code to a single generic class `MemoryDataSection.cs`.
* Updated `GameStatusChanged` event to fire once per state change (updated as regularly as `Graphics` packets are polled)

## Events

There are four events to listen for:

* AssettoCorsaCompetizione.StaticInfoUpdated
* AssettoCorsaCompetizione.GraphicsUpdated
* AssettoCorsaCompetizione.PhysicsUpdated
* AssettoCorsaCompetizione.GameStatusChanged

These events have individual timers and their respective update intervals can be changed to fit your own needs.

The default update intervals are:

```
AssettoCorsa.StaticInfoInterval: 5000 ms
AssettoCorsa.GraphicsInterval: 100 ms
AssettoCorsa.PhysicsInterval: 16 ms
```

The `AssettoCorsaCompetizione.Connect()` and `AssettoCorsaCompetizione.Stop()` functions are to connect and disconnect from the shared memory and also toggle the polling for the events.

After you've executed `Connect()` you can use `IsRunning` to check if it successfully connected to the shared memory.

Usage Example
-------------

In Visual Studio, you have two easy options:

1. Add the AssettoCorsaCompetizioneSharedMemory.dll as a reference to your project
2. Add the complete AssettoCorsaCompetizioneSharedMemory project to your solution and then add it as a reference

Here is a basic reader client that listens to all available events and posts information to the console

```C#
using AssettoCorsaCompetizioneSharedMemory;
using System;
using System.Threading;

namespace ACCReaderExample
{
    internal class Program
    {
        private static AssettoCorsaCompetizione _acc;

        static void Main(string[] args)
        {
            _acc = new AssettoCorsaCompetizione();
            _acc.StaticInfoInterval = 5000;
            _acc.PhysicsInterval = 16;
            _acc.GraphicsInterval = 100;

            _acc.StaticInfoUpdated += StaticInfoUpdated;
            _acc.PhysicsUpdated += PhysicsUpdated;
            _acc.GraphicsUpdated += GraphicsUpdated;
            _acc.GameStatusChanged += GameStatusChanged;

            var isRunning = false;

            while (!isRunning)
            {
                try
                {
                    _acc.Connect();
                }
                catch (GameNotStartedException) { }

                isRunning = _acc.IsRunning;
                Console.WriteLine("Waiting for ACC to start...");
                Thread.Sleep(1000);
            }
            Console.ReadKey();
        }

        private static void GameStatusChanged(object sender, GameStatusEventArgs e)
        {
            Console.WriteLine($"GameStatusChanged: {e.GameStatus}");
        }

        private static void StaticInfoUpdated(object sender, StaticInfoEventArgs e)
        {
            Console.WriteLine("StaticInfoUpdated");
        }

        private static void PhysicsUpdated(object sender, PhysicsEventArgs e)
        {
            Console.WriteLine("PhysicsUpdated");
            Console.WriteLine($"ID: {e.Physics.PacketId} - RPM: {e.Physics.Rpms} - Speed: {e.Physics.SpeedKmh} - Gear: {e.Physics.Gear}");
        }

        private static void GraphicsUpdated(object sender, GraphicsEventArgs e)
        {
            Console.WriteLine("GraphicsUpdated");
        }
    }
}

```
