using Qactive;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QactiveClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //var client = new TcpQbservableClient<long>(new IPEndPoint(IPAddress.Loopback, 3205));
            var client = new TcpQbservableClient<long>(new IPEndPoint(IPAddress.Loopback, 3205));

            IQbservable<long> query =
              from value in client.Query()
              where value <= 5 || value >= 8
              select value;

            using (query.Subscribe(
              value => Console.WriteLine("Client observed: " + value),
              ex => Console.WriteLine("Error: {0}", ex.Message),
              () => Console.WriteLine("Completed")))
            {
                Console.ReadKey();
            }
        }
    }
}
