using Qactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QactiveServerTest
{
    class Program
    {
        static void P2PNetworkTest(IPAddress serviceIP, IPAddress clientIP, int servicePort, int clientPort, string serviceId, string clientId)
        {
            IObservable<long> source = Observable.Interval(TimeSpan.FromSeconds(1));

            var service = source.ServeQbservableTcp(new IPEndPoint(serviceIP, servicePort));

            using (service.Subscribe(
              clientTermination => Console.WriteLine($"SERVER {serviceId}:  Client shutdown ..."),
              ex => Console.WriteLine("Fatal error: {0}", ex.Message),
              () => Console.WriteLine("This will never be printed because a service host never completes.")))
            {
                Console.WriteLine($"Server '{serviceId}' listening...");

                var client = new TcpQbservableClient<long>(new IPEndPoint(clientIP, clientPort));

                IQbservable<long> query =
                  from value in client.Query()
                  where value <= 5 || value >= 8
                  select value;

                using (query.Subscribe(
                  value => Console.WriteLine($"Client '{clientId}' observed: " + value),
                  ex => Console.WriteLine("Error: {0}", ex.Message),
                  () => Console.WriteLine("Completed")))
                {
                    Console.WriteLine("Client connecting ...");
                    Console.ReadKey();
                    Console.WriteLine("Client disconnecting ...");
                }

                Console.WriteLine("Client terminated.");

                Console.WriteLine("Press any key to shut down service.");
                Console.ReadKey();
            }
            Console.WriteLine("Service terminated.");
        }

        static void Main(string[] args)
        {
            Task.Run(() => P2PNetworkTest(IPAddress.Any, IPAddress.Loopback, 3205, 3206, "Node 1 server", "Node 1 client"));
            Task.Run(() => P2PNetworkTest(IPAddress.Any, IPAddress.Loopback, 3206, 3205, "Node 2 server", "Node 2 client"));
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
