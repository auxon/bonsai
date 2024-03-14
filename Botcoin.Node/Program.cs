using Bonsai.Observables.Blockchain;
using Qactive;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Bonsai.Node
{
    public static class Program
    {
        private static readonly CompositeDisposable Disposables = new CompositeDisposable(4);

        public static void Main(string[] args)
        {
            Console.Title = "Bonsai";

            Directory.CreateDirectory(Configuration.DataDirPath);

            Console.WriteLine("Mining ... hit CTRL-C to exit.");

            var (serviceIP, clientIP) = (IPAddress.Any, IPAddress.Loopback);
            var (servicePort, clientPort) = (3205, 3205);
            var (serviceEndPoint, clientEndPoint) = (new IPEndPoint(serviceIP, servicePort), new IPEndPoint(clientIP, clientPort));
            var (serviceId, clientId1, clientId2) = ("Server 1", "Client 1", "Client 2");

            var observableBlockchain = BlockGenerator<string>.Generate(Configuration.MAGIC_NUMBER, Configuration.DataDirPath);

            var serverDisposable =
                observableBlockchain
                .ServeQbservableTcp(new IPEndPoint(serviceIP, servicePort),
                new QbservableServiceOptions
                {
                    EvaluationContext = new ServiceEvaluationContext(typeof(IBlock<>)),
                    AllowExpressionsUnrestricted = true,
                    ExpressionOptions = ExpressionOptions.AllowAll,
                    SendServerErrorsToClients = true
                })
            .Do(t => Console.WriteLine(string.Join("\n", t.Exceptions.Select((ex, i) => $"Service error {i}:  ex.SourceException.Message"))))
            .Subscribe(onNext: t => Console.WriteLine($"Client terminated after {t.Duration}.  Reason:  {t.Reason}."),
                       onError: ex => Console.WriteLine("Fatal service error: {0}", ex.Message),
                       onCompleted: () => Console.WriteLine("This will never be printed because a service host never completes."));

            Disposables.Add(serverDisposable);

            var client1 = new TcpQbservableClient<IBlock<string>>(clientIP, clientPort, typeof(IBlock<>));

            var clientDisposable =
            (from block in client1.Query() select block).Subscribe(
            block => Console.WriteLine($"Client '{clientId1}' observed block:  {Environment.NewLine}{block}{Environment.NewLine}"),
            ex => Console.WriteLine($"Client '{clientId1}' Error: {ex.Message}"),
            () => Console.WriteLine($"Client '{clientId1}' disconnected."));

            Disposables.Add(clientDisposable);

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
            Console.WriteLine("Waiting for miner to stop after mining the next block ... ctrl-c to force abort.\n");
            Disposables.Dispose();
        }
    }
}
