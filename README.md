LevenshteinDFA
==============

Port's Lucene's Levenshtein DFA to .NET.

This is a fairly straightforward port with most parts ported to C#/.NET.

Example usage:

    // Construct:
    var auto = new LevenshteinAutomata("foobar", true).toAutomaton(1 /* distance 1 */);
    auto = Operations.determinize(auto);
    
    // Optionally minimize (removes cruft etc):
    auto = MinimizationOperations.minimize(auto);
    
    // Optimize for matching:
    var compiled = new CompiledAutomaton(auto);
    
    // Run/match:
    Assert.True(compiled.Matches("foebar"));

###Completely Unofficial Benchmarks###

The following is some (not very rigorous timing) on a reasonable laptop running Windows 8.1.

Running 

    var timer = Stopwatch.StartNew();
    
    for (var j = 0; j < numIterations; j++)
    {
        var auto2 = new LevenshteinAutomata(input, true).toAutomaton(1);
        auto2 = Operations.determinize(auto2);
        var comp = new CompiledAutomaton(auto2);

        Parallel.For(0, words.Length, i =>
        {
           if (comp.Matches(wordCps[i]))
              Interlocked.Increment(ref hitCount);
        });
    }
    
Here `wordCps` is a list of words that have been pre-computed as "codepoints".

The above executes in roughly 560 microseconds for a word list of 36,000 entries (averaged with over `numIterations = 10000`). This will depend on input but should give some indication of performance.
