using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidSharp;
using LuxaforSharp.Commands;

namespace LuxaforSharp
{
    /// <summary>
    /// Represents a real Luxafor device.
    /// </summary>
    public class Device : BaseDevice
    {
        private readonly HidDevice device;
        private HidStream stream;

        public string DevicePath 
        {
            get { return this.device.DevicePath; } 
        }

        public Device(HidDevice device)
        {
            this.device = device;
            this.stream = this.device.Open();
        }
        
        /// <summary>
        /// Dispose the device.
        /// </summary>
        public override void Dispose()
        {
            this.stream.Dispose();
        }

        /// <summary>
        /// Low level method allowing you to send raw commands to the device.
        /// Implements ICommand to send custom commands to this method
        /// </summary>
        /// <param name="command">Command to send to the device</param>
        /// <param name="timeout">Time, in milliseconds, after which the application should stop waiting for the acknowledgment of this message</param>
        /// <returns>Task representing the operation. Result is true if the message has been acknowledged, false otherwise</returns>
        public override Task<bool> SendCommand(ICommand command, int timeout = 0)
        {
            var task = new Task<Task<bool>>(async () =>
            {
                this.stream.WriteTimeout = timeout == 0 ? Timeout.Infinite : timeout;
                await this.stream.WriteAsync(command.Bytes, 0, command.Bytes.Length);
                return true;
            });

            task.Start();
            task.Wait();
            return task.Result;
        }
    }
}
