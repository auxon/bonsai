using Abacus.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Abacus.Observables.Blockchain
{
    public static class BlockGenerator<T>
    {
        private static long difficulty = 1;

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

            Func<long> calculateDifficulty = () => difficulty >= 4 ? 4 : ++difficulty;  // TODO:  actually calculate it.

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
                var currentDifficulty = calculateDifficulty();

                Console.WriteLine($"\nDifficulty: { currentDifficulty }.");
                Console.WriteLine($"Hashing ...");
                do
                {
                    var toHashPlusNonce = toHash.Union(BitConverter.GetBytes(nonce++));
                    hash = sha256.ComputeHash(toHashPlusNonce.ToArray());
                    hexHash = hash.BytesToHex();
                    Console.CursorLeft = 0;
                    Console.Write((hexHash, nonce));
                } while (!hexHash.StartsWith("0".Repeat(currentDifficulty)));
                
                Console.ForegroundColor = ConsoleColor.Yellow; Console.BackgroundColor = ConsoleColor.Black;
                Console.WriteLine($"\nBlock found! {(hexHash, nonce).ToString()}");
                Console.ResetColor();

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
                var block = new Block<T>
                {
                    Height = previousBlock.GetHeight() + 1,
                    Difficulty = difficulty,
                    PreviousBlockHash = previousBlock.GetHash(),
                    Data = data
                };

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
            loadGenesisChain = () =>
            {
                //TODO:  Implement loading.
                return null;

                //if (File.Exists(Configuration.DataFilePath))
                //{
                //    using (var reader = new BinaryReader(File.OpenRead(Configuration.DataFilePath)))
                //    {
                //        var size = default(Block<string>).Serialize().Count();
                //        var readBytes = new byte[size];
                //        do
                //        {

                //        } while (reader.Read() > 0);
                //        return Observable.Return<IBlock<T>>(default(Block<T>)); // TODO:  Finish this.
                //    }
                //}
                //else
                //{
                //    return Observable.Empty<IBlock<T>>();
                //}
            };

            Func<T> getData = () => default(T); // TODO:  Implement.

            var blockchain = generateBlockchain(loadGenesisChain() ?? createGenesisChain(),
                             chain => chain.Zip(chain.Skip(1), (previous, latest) => createBlock(latest, getData())));

            return blockchain;
        }
    }
}
