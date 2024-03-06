using Abacus.Extensions;
using Abacus.Observables.Blockchain;
using Qactive;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Abacus
{
    internal class AbacusEvaluator : LocalEvaluator
    {
        public override Expression GetValue(PropertyInfo property, MemberExpression member, ExpressionVisitor visitor, IQbservableProtocol protocol)
        {
            throw new NotImplementedException();
        }

        public override Expression GetValue(FieldInfo field, MemberExpression member, ExpressionVisitor visitor, IQbservableProtocol protocol)
        {
            throw new NotImplementedException();
        }

        public override Expression Invoke(MethodCallExpression call, ExpressionVisitor visitor, IQbservableProtocol protocol)
        {
            throw new NotImplementedException();
        }

        protected override Either<object, Expression> TryEvaluateEnumerable(string name, object value, Type type, IQbservableProtocol protocol)
        {
            throw new NotImplementedException();
        }

        protected override Expression TryEvaluateObservable(string name, object value, Type type, IQbservableProtocol protocol)
        {
            throw new NotImplementedException();
        }
    }

    public static class Program
    {
        private static readonly CompositeDisposable Disposables = new CompositeDisposable(4);

        private static IQbservable<IBlock<T>> StartClient<T>(IPAddress clientIP, int clientPort, string clientId)
        {
            var client = new TcpQbservableClient<IBlock<T>>(clientIP, clientPort, typeof(IBlock<>));

            var query = from block in client.Query() select block;

            return query;
        }

        public static void Main(string[] args)
        {
            Console.Title = "Abacus";

            Directory.CreateDirectory(Configuration.DataDirPath);

            Console.WriteLine("Mining ... hit CTRL-C to exit.");


            var blockObservable = BlockGenerator<Block<BSV>>
                .Generate(Configuration.MAGIC_NUMBER, Configuration.DataDirPath)
                .Do(block =>
                    Console.WriteLine($"Block: {((Block<string>)block).Height}, Hash: {((Block<string>)block).Hash}, Previous Hash: {((Block<string>)block).PreviousBlockHash}"))
                .Do(block =>
                {
                    var sb = block.Serialize();
                    using (var fs = File.Open(Path.Combine(Configuration.DataDirPath, "db.abc"), FileMode.Append))
                        fs.Write(sb.ToArray(), 0, sb.Count());
                });


            var (serviceIP, clientIP) = (IPAddress.Any, IPAddress.Loopback);
            var (servicePort, clientPort) = (3205, 3205);
            var (serviceEndPoint, clientEndPoint) = (new IPEndPoint(serviceIP, servicePort), new IPEndPoint(clientIP, clientPort));
            var (serviceId, clientId1, clientId2) = ("Server 1", "Client 1", "Client 2");

            IDisposable Start<T>(TraceSource trace)
            {
                var svcOptions = new QbservableServiceOptions() {
                    SendServerErrorsToClients = true,
                    AllowExpressionsUnrestricted = true,
                    EnableDuplex = true,
                    ExpressionOptions = ExpressionOptions.AllowAll,
                    EvaluationContext = new ServiceEvaluationContext(typeof(IBlock<>))
                };

                var blockOb = BlockGenerator<T>.Generate(Configuration.MAGIC_NUMBER, Configuration.DataDirPath)
                    .ServeQbservableTcp(serviceEndPoint, svcOptions);

                //var svc = Qactive.TcpQbservableServer.CreateService<IBlock<T>, IBlock<T>>(
                //  new IPEndPoint(serviceIP, servicePort), svcOptions, bo => blockOb.Do(block => Console.WriteLine("Service served block: {0}", block.GetHash())));
                
                return blockOb.Subscribe(
                  terminatedClient =>
                  {
                      foreach (var ex in terminatedClient.Exceptions)
                      {
                          Console.WriteLine("Service error: {0}", ex.SourceException.Message);
                      }

                      Console.WriteLine("Client shutdown: " + terminatedClient);
                  },
                  ex => Console.WriteLine("Fatal service error: {0}", ex.Message),
                  () => Console.WriteLine("This will never be printed because a service host never completes."));
            }

            using (var serviceDisposable = Start<string>(new TraceSource("Abacus", SourceLevels.Information)))
            {

                //var service = blockObservable.ServeQbservableTcp(new IPEndPoint(serviceIP, servicePort), new QbservableServiceOptions
                //{
                //    EvaluationContext = new ServiceEvaluationContext(typeof(IBlock<>)),
                //    AllowExpressionsUnrestricted = true,
                //    ExpressionOptions = ExpressionOptions.AllowAll,
                //    SendServerErrorsToClients = true
                //});

                //Disposables.Add(serviceDisposable);

                var client1 = new TcpQbservableClient<IBlock<string>>(clientIP, clientPort, typeof(IBlock<>));

                //var client1 = new TcpQbservableClient<IBlock<string>>(clientIP, clientPort, typeof(IBlock<>));

                var query = from block in client1.Query()
                            select block;

                using (query.Subscribe(
                        block => Console.WriteLine($"Client '{clientId1}' observed block #" + block),
                        ex => Console.WriteLine($"Client '{clientId1}' Error: {ex.Message}"),
                        () => Console.WriteLine($"Client '{clientId1}' disconnected.")))
                {

                    //var client1Disposable =
                    //    query.Subscribe(
                    //        block => Console.WriteLine($"Client '{clientId1}' observed block #" + block.GetHeight()),
                    //        ex => Console.WriteLine($"Client '{clientId1}' Error: {ex.Message}"),
                    //        () => Console.WriteLine($"Client '{clientId1}' disconnected."));

                    //var client2 = StartClient<string>(clientIP, clientPort, clientId2);
                    //var client2Disposable =
                    //    client2.Select(b => b).Subscribe(
                    //        block => Console.WriteLine($"Client '{clientId2}' observed block #" + block.GetHeight()),
                    //        ex => Console.WriteLine($"Client '{clientId2}' Error: {ex.Message}"),
                    //        () => Console.WriteLine($"Client '{clientId2}' disconnected."));

                    Console.WriteLine("\nPress any key to exit.");
                    Console.ReadKey();
                }
            }
            
        }
    }
}
