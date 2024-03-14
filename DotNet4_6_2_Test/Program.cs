using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Qactive;

namespace Abacus
{
    public static class Configuration
    {
        public const int MAGIC_NUMBER = 0x07770777;
        public static string AppDataPath => Environment.GetEnvironmentVariable("APPDATA") ?? Environment.CurrentDirectory;
        public static string DataDirPath => Environment.GetEnvironmentVariable("ABACUS_DATADIR") ?? Path.Combine(AppDataPath, "abacus", "datadir");
        public static string DataFilePath => Path.Combine(Configuration.DataDirPath, "db.abc");
        public static IPEndPoint EndPoint => new IPEndPoint(IPAddress.Any, port: 8080);
        public static IPEndPoint ClientEndPoint => new IPEndPoint(IPAddress.Parse("127.0.0.1"), port: 8081);
    }

    public static class Program
    {
        public static void Main()
        {
            Directory.CreateDirectory(Configuration.DataDirPath);

            Console.WriteLine("Mining ... hit CTRL-C to exit.");

            
            var blockObservable = BlockGenerator<string>.Generate(Configuration.MAGIC_NUMBER, Configuration.DataDirPath)
                .Do(block =>
                    Console.WriteLine($"Block: {((Block<string>)block).Height}, Hash: {((Block<string>)block).Hash}, Previous Hash: {((Block<string>)block).PreviousBlockHash}"))
                .Do(block => {
                    var sb = block.Serialize();
                    using (var fs = File.Open(Path.Combine(Configuration.DataDirPath, "db.abc"), FileMode.Append))
                        fs.Write(sb, 0, sb.Length);
                });

            // Set up networking.
            ////var service = Qactive.TcpQbservableServer.CreateService<IBlock<string>, IBlock<string>>(new AppDomainSetup(), Configuration.EndPoint, (block) => blockObservable);

            // alternatively ...
            var service = blockObservable.ServeQbservableTcp(Configuration.EndPoint, QbservableServiceOptions.Default);

            using (service.Subscribe((ct) => Console.WriteLine($"Client shutdown.  Reason: {ct.Reason}"), (ex) => Console.WriteLine($"Error: {Environment.NewLine + ex.Message}"), () => Console.WriteLine("Should never complete.")))
            {
                Console.WriteLine($"Service listening on port {Configuration.EndPoint}.");
                Console.WriteLine("Press any key to shut down the service.");
                blockObservable.Subscribe();
                Console.ReadKey();
            }

            var client = new TcpQbservableClient<Block<string>>(Configuration.ClientEndPoint);
            
           
            IQbservable<Block<string>> query =
              from block in client.Query()
              where block.Height > -1
              select block;

            using (query.Subscribe(
              value => Console.WriteLine("Client observed: " + value.ToString()),
              ex => Console.WriteLine("Error: {0}", ex.Message),
              () => Console.WriteLine("Completed")))
            {
                Console.ReadKey();
            }

            //blockObservable.Subscribe();
            //Console.ReadKey();
        }
    }

    public interface IBlock<T>
    {
        long GetHeight();
        string GetHash();
        T SetData(T input);
        T GetData();
    }

    [Serializable]
    public struct Block<T> : IBlock<T>
    {
        public int MagicNumber => Configuration.MAGIC_NUMBER;
        public long Height;
        public long Difficulty;
        public string Hash;
        public string PreviousBlockHash;
        public double Nonce;
        public DateTime Timestamp;
        public T Data;

        public long GetHeight() => this.Height;
        public string GetHash() => this.Hash;
        public T GetData() => this.Data;
        public T SetData(T input) => this.Data = input;
    }

    public class Blockchain<T> : List<Block<T>> { }

    public static class BlockGenerator<T>
    {
        public static IObservable<IBlock<T>> Generate(int magicNumber, string datadir)
        {
            // Generates the blockchain.
            Func<IObservable<IBlock<T>>,
            Func<IObservable<IBlock<T>>, IObservable<IBlock<T>>>, IObservable<IBlock<T>>>
            generateBlockchain =
            (genesis, function) => Observable.Create<IBlock<T>>(observer =>
            {
                var subject = new Subject<IBlock<T>>();
                var loopFunc = function(subject.ObserveOn(NewThreadScheduler.Default));
                var loopSub = loopFunc.Subscribe(i => subject.OnNext(i));
                var outerSub = subject.Subscribe(i => observer.OnNext(i));
                genesis.Subscribe(subject.OnNext);
                return new CompositeDisposable(2) { loopSub, outerSub };
            });

            Func<long> calculateDifficulty = () => 4;  // TODO:  actually calculate it.

            Func<long, long, string, T, double,
            (byte[], double)>
            calculateHash = (height, difficulty, previousBlockHash, data, nonce) =>
            {
                var sha256 = System.Security.Cryptography.SHA256.Create();
                sha256.Initialize();

                var toHash = BitConverter.GetBytes(magicNumber)
                            .Union(BitConverter.GetBytes(height))
                            .Union(BitConverter.GetBytes(difficulty))
                            .Union(previousBlockHash?.HexToBytes() ?? BitConverter.GetBytes(0))
                            .Union(data?.Serialize() ?? default(T)?.Serialize() ?? 0.Serialize());

                byte[] hash;
                string hexHash;
                do
                {
                    var toHashPlusNonce = toHash.Union(BitConverter.GetBytes(nonce++));
                    hash = sha256.ComputeHash(toHashPlusNonce.ToArray());
                    hexHash = hash.BytesToHex();
                } while (!hexHash.StartsWith("0".Repeat(calculateDifficulty())));

                return (hash, nonce);
            };

            Func<double> getInitialNonce = () => new System.Random().NextDouble();

            //            Func<IObservable<IBlock<T>>, T,
            //            IObservable<IBlock<T>>>
            //            createBlock = (previousBlocks, data) =>
            //			{
            //				return previousBlocks.Select(previousBlock =>
            //				{
            //					var block = new Block<T>();
            //					block.Height = previousBlock.GetHeight() + 1;
            //					block.Difficulty = calculateDifficulty();
            //					block.PreviousBlockHash = previousBlock.GetHash();
            //					block.Data = data;
            //					var (hash, nonce) = calculateHash(block.Height, block.Difficulty, block.PreviousBlockHash, block.Data, getInitialNonce());
            //					block.Hash = hash.BytesToHex();
            //					block.Nonce = nonce;
            //					return block;
            //				});
            //            };

            Func<IBlock<T>, T,
            IBlock<T>>
            createBlock = (previousBlock, data) =>
            {
                var block = new Block<T>();
                block.Height = previousBlock.GetHeight() + 1;
                block.Difficulty = calculateDifficulty();
                block.PreviousBlockHash = previousBlock.GetHash();
                block.Data = data;
                var (hash, nonce) = calculateHash(block.Height, block.Difficulty, block.PreviousBlockHash, block.Data, getInitialNonce());
                block.Hash = hash.BytesToHex();
                block.Nonce = nonce;
                block.Timestamp = DateTime.UtcNow;
                return block;
            };

            Func<IObservable<IBlock<T>>>
            createGenesisChain = () =>
            {
                var genesis = createBlock(default(Block<T>), default(T));
                var exodus = createBlock(genesis, default(T));
                return Observable.Return(genesis).Concat(Observable.Return(exodus));
            };

            Func<IObservable<IBlock<T>>>
            loadGenesisChain = () => {
                return null;
                // TODO:  complete this
                //if (File.Exists(Configuration.DataFilePath))
                //{
                //    using (var reader = new BinaryReader(File.OpenRead(Configuration.DataFilePath)))
                //    {
                //        var size = default(Block<string>).Serialize().Length;
                //        var readBytes = new byte[size];
                //        do
                //        {

                //        } while (reader.Read() > 0);
                //        return Observable.Return<IBlock<T>>(default(Block<T>)); // TODO:  Finish this.
                //    }
                //}
                //else
                //{
                //    return null;
                //}
            };

            Func<T> getData = () => default(T); // TODO:  Implement.

            var blockchain = generateBlockchain(loadGenesisChain() ?? createGenesisChain(),
                             chain => chain.Zip(chain.Skip(1), (previous, latest) => createBlock(latest, getData())));

            return blockchain;
        }
    }

    public static class ByteAndHexExtensions
    {
        //TODO:  Validate string input, and deal with upper and lowercase variants.
        //TODO:  Optimize.
        public static byte[] HexToBytes(this string input)
        {
            input = input.Replace("-", "");
            var outputLength = input.Length / 2;
            var output = new byte[outputLength];
            for (var i = 0; i < outputLength; i++)
                output[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
            return output;
        }

        public static string BytesToHex(this byte[] input)
        {
            return BitConverter.ToString(input).Replace("-", "");
        }

        public static byte[] Serialize(this object o)
        {
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                IFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, o);
                bytes = ms.ToArray();
            }
            return bytes;
        }

        public static T Deserialize<T>(this byte[] bytes)
        {
            T val;
            using (var ms = new MemoryStream(bytes))
            {
                IFormatter bf = new BinaryFormatter();
                val = (T)bf.Deserialize(ms);
            }
            return val;
        }
    }

    public static class StringExtensions
    {
        public static string Repeat(this string input, long number)
        {
            var sb = new StringBuilder(input);
            for (int i = 0; i < number - 1; i++)
            {
                sb.Append(input);
            }
            return sb.ToString();
        }
    }
}
