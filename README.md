LevenshteinDFA
==============

Port's Lucene's Levenshtein DFA to .NET.

This is a fairly straightforward port with most parts ported to C#/.NET.

Example usage:

    var auto = Operations.determinize(new LevenshteinAutomata("foobar", true).toAutomaton(1 /* distance 1 */));
    auto = MinimizationOperations.minimize(auto);
    var compiled = new CompiledAutomaton(auto);
    
    Assert.True(compiled.Matches("foebar"));
