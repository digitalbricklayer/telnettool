using System;
using System.Diagnostics;
using PrimS.Telnet;

namespace AviosysTelnetTool
{
    /// <summary>
    /// Entry point for the Aviosys telnet tool.
    /// </summary>
    class Program
    {
        private const int TimeoutMs = 100;

        static void Main(string[] args)
        {
            using (Client client = new Client("10.0.0.81", 23, new System.Threading.CancellationToken()))
            {
                Debug.Assert(client.IsConnected);

                string welcomeMessage = client.TerminatedRead("->", TimeSpan.FromMilliseconds(TimeoutMs));
                Console.Out.WriteLine(welcomeMessage);
                Debug.Assert(welcomeMessage.TrimEnd(' ').EndsWith("->"));

                // Log into the unit using default user name and password
                client.WriteLine("admin:12345678");
                string loginResponse = client.TerminatedRead("->", TimeSpan.FromMilliseconds(TimeoutMs));
                Console.Out.WriteLine(loginResponse);
                Debug.Assert(loginResponse.TrimEnd(' ').EndsWith("->"));

                // Send the help command
                client.WriteLine("help");
                string helpResponse = client.TerminatedRead("->", TimeSpan.FromMilliseconds(TimeoutMs));
                Console.Out.WriteLine(helpResponse);
            }

            Console.Out.WriteLine("Press [Enter] to terminate.");
            Console.In.ReadLine();
        }
    }
}
