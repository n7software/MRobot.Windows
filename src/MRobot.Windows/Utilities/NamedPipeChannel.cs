using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using log4net;

namespace MRobot.Windows.Utilities
{
    public static class NamedPipeChannel
    {
        private static ILog Log = LogManager.GetLogger("NamedPipeChannel");

        public static Task BeginServerListen(string pipeName, NamedPipeChannelReceiveMessage onMessageReceived)
        {
            if (string.IsNullOrEmpty(pipeName)) { throw new ArgumentNullException("pipeName");}
            if (onMessageReceived == null) { throw new ArgumentNullException("onMessageReceived");}

            return Task.Run(() =>
            {
                try
                {
                    using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In))
                    {
                        while (true)
                        {
                            try
                            {
                                pipeServer.WaitForConnection();
                                var reader = new StreamReader(pipeServer);

                                string message = reader.ReadToEnd();
                                pipeServer.Disconnect();

                                Task.Run(() => onMessageReceived(pipeName, message));
                            }
                            catch (Exception exc)
                            {
                                Log.Debug(string.Format("Receiving message on: {0}", pipeName), exc);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    Log.Error(string.Format("Setting up server: {0}", pipeName), exc);
                }
            });
        }

        public static Task<bool> SendMessageToServer(string pipeName, string message)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out))
                    {
                        pipe.Connect(1000);

                        using (var stream = new StreamWriter(pipe))
                        {
                            stream.Write(message);
                            stream.Flush();
                        }
                    }

                    return true;
                }
                catch (TimeoutException)
                {
                }
                catch (Exception exc)
                {
                    Log.Debug(string.Format("Sending message on: {0}", pipeName), exc);
                }

                return false;
            });
        }
    }

    public delegate void NamedPipeChannelReceiveMessage(string pipName, string message);
}
