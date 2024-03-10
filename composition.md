# Composing Multiple Blockchains in Bonsai

Abacus provides the functionality to create individual `Blockchain<T>` instances for various cryptocurrencies. Additionally, Abacus enables the composition of these blockchains into a single, cohesive data stream. This document outlines the process of composing multiple blockchains and the subsequent handling of their data.

## Composing Blockchains

By leveraging Reactive Extensions (Rx), Abacus can combine several observables into one, allowing for simultaneous observation and processing of multiple blockchain instances.

### Example

Here's a simplified example showcasing how to compose multiple `Blockchain<T>` instances:

```csharp
using Abacus.Observables.Blockchain;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Abacus
{
    public static class BlockchainComposer
    {
        public static void Main(string[] args)
        {
            // Compose multiple blockchain observables
            var composedChains = ComposeBlockchains(
                Configuration.BSV_MagicNumber,
                Configuration.BCH_MagicNumber,
                Configuration.BTC_MagicNumber
            );

            // Subscribe to the composed observable to process incoming blocks
            composedChains.Subscribe(composedBlock =>
            {
                // Handle the composed block data here
                Console.WriteLine($"Composed Block from {composedBlock.Item1.Symbol}: Height = {composedBlock.Item1.Height}, Hash = {composedBlock.Item1.Hash}");
                Console.WriteLine($"Composed Block from {composedBlock.Item2.Symbol}: Height = {composedBlock.Item2.Height}, Hash = {composedBlock.Item2.Hash}");
                Console.WriteLine($"Composed Block from {composedBlock.Item3.Symbol}: Height = {composedBlock.Item3.Height}, Hash = {composedBlock.Item3.Hash}");
            });

            Console.WriteLine("Observing composed blockchains. Press any key to exit.");
            Console.ReadKey();
        }

        private static IObservable<(IBlock<BSV>, IBlock<BCH>, IBlock<BTC>)> ComposeBlockchains(uint bsvMagicNumber, uint bchMagicNumber, uint btcMagicNumber)
        {
            // Assume BlockGenerator.Generate returns an IObservable<IBlock<T>> for the given magic number
            var bsvBlocks = BlockGenerator<Block<BSV>>.Generate(bsvMagicNumber, Configuration.DataDirPath);
            var bchBlocks = BlockGenerator<Block<BCH>>.Generate(bchMagicNumber, Configuration.DataDirPath);
            var btcBlocks = BlockGenerator<Block<BTC>>.Generate(btcMagicNumber, Configuration.DataDirPath);

            // Use Observable.Zip to combine multiple observables
            return Observable.Zip(bsvBlocks, bchBlocks, btcBlocks, (bsv, bch, btc) => (bsv, bch, btc));
        }
    }
}
```

In this example, `ComposeBlockchains` is a method that takes the magic numbers for each respective blockchain and returns a zipped observable stream that emits tuples of blocks from each blockchain as they are produced. Subscribers to this stream receive updates from all three blockchains in real-time, allowing for complex operations such as cross-chain analysis or aggregate ledger updates.

## Considerations

When composing multiple blockchains:

- **Synchronization**: Ensure that the timing of blocks from different chains is accounted for, as blocks may not be produced simultaneously across chains.
- **Error Handling**: Implement comprehensive error handling to manage potential discrepancies or failures in one or more blockchains.
- **Performance**: Consider the performance implications of composing multiple streams, as this could introduce overhead in data processing and memory usage.
- **Resource Management**: Make sure to dispose of subscriptions properly to free up resources and prevent memory leaks.

## Conclusion

The composition of multiple blockchain streams in Abacus unlocks the potential for sophisticated multi-chain applications. By abstracting the complexity of individual blockchain operations into a unified reactive model, developers can focus on building innovative features that span across different cryptocurrencies.
