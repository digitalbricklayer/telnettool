using System;
using System.Diagnostics;
using System.Threading;
using PrimS.Telnet;

namespace AviosysTelnetTool
{
    /// <summary>
    /// Entry point for the Aviosys telnet tool.
    /// </summary>
    class Program
    {
        private const int ReadTimeoutInMs = 100;

        static void Main(string[] args)
        {
            using (Client telnetClient = new Client("10.0.0.81", 23, new System.Threading.CancellationToken()))
            {
                Debug.Assert(telnetClient.IsConnected);

                var startTime = DateTime.UtcNow;
                Console.Out.WriteLine("Test starting at {0}", startTime.ToShortTimeString());

                ReadWelcomeMessage(telnetClient);
                LoginToRemote(telnetClient);

                // Continually get the power status
                while (true)
                {
                    try
                    {
                        telnetClient.WriteLine("getpower");
                        string getPowerResponse = telnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(ReadTimeoutInMs));
                        Console.Out.WriteLine(getPowerResponse);
                    }
                    catch (Exception)
                    {
                        var testDuration = DateTime.UtcNow - startTime;
                        Console.Error.WriteLine("Test terminating at {0} duration {1}", startTime.ToShortTimeString(), testDuration);
                        break;
                    }
                    Thread.Sleep(1000);
                }
            }

            Console.Out.WriteLine("Press [Enter] to terminate.");
            Console.In.ReadLine();
        }

        private static void LoginToRemote(Client telnetClient)
        {
            // Log into the unit using default user name and password
            telnetClient.WriteLine("admin:12345678");
            string loginResponse = telnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(ReadTimeoutInMs));
            Console.Out.WriteLine(loginResponse);
            Debug.Assert(loginResponse.TrimEnd(' ').EndsWith("->"));
        }

        private static void ReadWelcomeMessage(Client telnetClient)
        {
            string welcomeMessage = telnetClient.TerminatedRead("->", TimeSpan.FromMilliseconds(ReadTimeoutInMs));
            Console.Out.WriteLine(welcomeMessage);
            Debug.Assert(welcomeMessage.TrimEnd(' ').EndsWith("->"));
        }
    }
}
