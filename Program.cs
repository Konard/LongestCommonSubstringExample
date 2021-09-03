using System;
using System.Collections.Generic;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.Memory;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.Frequencies.Cache;
using Platform.Data.Doublets.Sequences.Frequencies.Counters;
using Platform.Data.Doublets.Sequences.Indexes;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Memory;

namespace LongestCommonSubstringExample
{
    class Program
    {
        class StringToUnicodeSymbolsListConverter<TLink> : IConverter<string, IList<TLink>>
        {
            private readonly IConverter<char, TLink> _charToUnicodeSymbolConverter;

            public StringToUnicodeSymbolsListConverter(IConverter<char, TLink> charToUnicodeSymbolConverter) => _charToUnicodeSymbolConverter = charToUnicodeSymbolConverter;

            public IList<TLink> Convert(string source)
            {
                var elements = new TLink[source.Length];
                for (int i = 0; i < elements.Length; i++)
                {
                    elements[i] = _charToUnicodeSymbolConverter.Convert(source[i]);
                }
                return elements;
            }
        }

        static void Main(string[] args)
        {
            var constants = new LinksConstants<ushort>(enableExternalReferencesSupport: true);
            using var memory = new HeapResizableDirectMemory();
            // AVL tree based index reduces the maximum available links address space,
            // because it uses some additional bits to store data required for balancing and threads support.
            // So in case we use small address space, like ushort (65536 maximum links) it is we have to set useAvlBasedIndex into false.
            // Enabling of external references further cuts this address space in half.
            // So the actual result is that we have 32768 maximum links and the same amount of external references.
            // With useAvlBasedIndex set to true, we would have only 1024 maximum links in the links' storage (cuts out 5 bits from address space). 
            // External references will be used to store unicode symbol codes.
            using var disposableLinks = new UnitedMemoryLinks<ushort>(memory, UnitedMemoryLinks<ushort>.DefaultLinksSizeStep, constants, IndexTreeType.SizedAndThreadedAVLBalancedTree);
            var links = disposableLinks.DecorateWithAutomaticUniquenessAndUsagesResolution();

            var addressToRawNumberConvert = new AddressToRawNumberConverter<ushort>();

            var meaningRoot = links.GetOrCreate<ushort>(1, 1);
            var unicodeSymbolMarker = links.GetOrCreate(meaningRoot, addressToRawNumberConvert.Convert(1));
            var unicodeSequenceMarker = links.GetOrCreate(meaningRoot, addressToRawNumberConvert.Convert(2));

            // Counts total amount of sequences in which the symbol (link) is used.
            var totalSequenceSymbolFrequencyCounter = new TotalSequenceSymbolFrequencyCounter<ushort>(links);
           
            var linkFrequenciesCache = new LinkFrequenciesCache<ushort>(links, totalSequenceSymbolFrequencyCounter);
            
            var linkToItsFrequencyNumberConverter = new FrequenciesCacheBasedLinkToItsFrequencyNumberConverter<ushort>(linkFrequenciesCache);
            var sequenceToItsLocalElementLevelsConverter = new SequenceToItsLocalElementLevelsConverter<ushort>(links, linkToItsFrequencyNumberConverter);
            
            var optimalVariantConverter = new OptimalVariantConverter<ushort>(links, sequenceToItsLocalElementLevelsConverter);

            var charToUnicodeSymbolConverter = new CharToUnicodeSymbolConverter<ushort>(links, addressToRawNumberConvert, unicodeSymbolMarker);

            var frequencyIncrementingSequenceIndex = new CachedFrequencyIncrementingSequenceIndex<ushort>(linkFrequenciesCache);

            var stringToUnicodeSymbolsListConverter = new StringToUnicodeSymbolsListConverter<ushort>(charToUnicodeSymbolConverter);

            var stringToUnicodeSequenceConverter = new StringToUnicodeSequenceConverter<ushort>(links, charToUnicodeSymbolConverter, new Unindex<ushort>(), optimalVariantConverter, unicodeSequenceMarker);

            // Calculate frequences

            var first = "ABABC";
            var second = "BABCA";

            var firstUnicodeSymbolsList = stringToUnicodeSymbolsListConverter.Convert(first);
            var secondUnicodeSymbolsList = stringToUnicodeSymbolsListConverter.Convert(second);

            frequencyIncrementingSequenceIndex.Add(firstUnicodeSymbolsList);
            frequencyIncrementingSequenceIndex.Add(secondUnicodeSymbolsList);

            // Create sequences

            var firstUnicodeSequence = stringToUnicodeSequenceConverter.Convert(first);
            var secondUnicodeSequence = stringToUnicodeSequenceConverter.Convert(second);

            foreach(var link in links.All())
            {
                Console.WriteLine(links.Format(link));
            }
        }
    }
}
