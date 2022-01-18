# WordSquare

![](docs/images/WordSquare.png)

This is a very quick proof-of-concept for procedurally generated word grids. It uses the [Typocalypse.Trie](https://geekyisawesome.blogspot.com/2010/07/typocalypse.html) classes from the [Typocalypse repo](https://code.google.com/archive/p/typocalypse/source/default/source). Word squares are generated at runtime, and the pool of words comes from a text file. The one used here are [the first 1000 basic US English words](https://gist.github.com/deekayen/4148741).

## Issues

The words that come back from the solver are treated as a Dictionary with unique entries, but you can actually have multiple instances of the same word.
