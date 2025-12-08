# SHISHUA.NET

> [!WARNING]  
> SHISHUA should not be used for cryptographic purposes!

A .NET reimplementation of [SHISHUA](https://github.com/espadrine/shishua), the 2025 world record holder for the fastest pRNG ever created. It is an incredibly efficient and equally resilient randomizer that is especially useful in cases where a lot of data needs to be generated in bulk.

This port was originally made for [The Conservatory](https://xansworkshop.com/conservatory), an indie game by Xan's Workshop, but was ultimately deemed useful to the general public and was thus released here under the same license (CC0) as the original algorithm for free public use.

# Supported Variations

Implementations of the algorithm are as follows. Note that the current library will attempt to use the most powerful technique available for the hardware running the code.

- [x] Scalar (All Architectures)
- [x] SSE2 (x86)
- [x] AVX2 (x86)
- [ ] Neon (ARM)[^1]
- [ ] Scalar, Half Size[^2]
- [ ] SSE2, Half Size[^2]
- [ ] AVX2, Half Size[^2]
- [ ] Neon, Half Size[^1],[^2]

[^1]: Neon is not supported in .NET at this time.
[^2]: Half Size implementations were not useful when this library was designed for private use originally, and they have not been added yet as a result.