using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SHISHUA_CSHARP {
	partial class SHISHUA {

		// Scalar / "Universal" implementation.
		/// <summary>
		/// The main routine of SHISHUA Scalar, translated to C#.
		/// </summary>
		/// <param name="state">The randomizer state.</param>
		/// <param name="resultBuffer">The output buffer to store generated random bytes into. Can be <see langword="null"/> to skip storing data and advance the state anyway.</param>
		/// <param name="generationSize">The amount of bytes to generate. If the <paramref name="resultBuffer"/> is not <see langword="null"/> (or, empty), this must match its size. Must be divisible by 128.</param>
		private static unsafe void Generate_Scalar(ref PrngState state, Span<byte> resultBuffer, int generationSize) {
			if (!resultBuffer.IsEmpty) {
				if (resultBuffer.Length != generationSize) throw new ArgumentException($"The {nameof(generationSize)} parameter must be equal to {nameof(resultBuffer)}.Length");
				if (generationSize % 128 != 0) throw new ArgumentException($"The {nameof(generationSize)} parameter (and by extension {nameof(resultBuffer)}.Length) must be divisible by 128.");
			}

			Span<ulong> t = stackalloc ulong[8];
			ReadOnlySpan<byte> shuf_offsets = [
				2,3,0,1, 5,6,7,4, // left
				3,0,1,2, 6,7,4,5  // right
			];

			int bufOffset = 0;
			for (int i = 0; i < generationSize; i += 128) {
				if (!resultBuffer.IsEmpty) {
					for (int j = 0; j < 16; j++) {
						WriteToSpanAlwaysLE(resultBuffer[bufOffset..], state.scalar_output[j]);
						bufOffset += 8;
					}
				}

				for (int j = 0; j < 2; j++) {
					fixed (ulong* s = &state.scalar_state[j * 8]) {
						fixed (ulong* o = &state.scalar_output[j * 4]) {
							for (int k = 0; k < 4; k++) {
								s[k + 4] += state.scalar_counter[k];
							}
							for (int k = 0; k < 8; k++) {
								t[k] = (s[shuf_offsets[k]] >> 32) | (s[shuf_offsets[k + 8]] << 32);
							}

							for (int k = 0; k < 4; k++) {
								ulong u_lo = s[k + 0] >> 1;
								ulong u_hi = s[k + 4] >> 3;
								s[k + 0] = u_lo + t[k + 0];
								s[k + 4] = u_hi + t[k + 4];
								o[k] = u_lo ^ t[k + 4];
							}
						}
					}
				}

				for (int j = 0; j < 4; j++) {
					state.scalar_output[j + 8] = state.scalar_state[j + 0] ^ state.scalar_state[j + 12];
					state.scalar_output[j + 12] = state.scalar_state[j + 8] ^ state.scalar_state[j + 4];
					state.scalar_counter[j] += (ulong)(7 - (j * 2));
				}
			}

		}

		private static unsafe PrngState Initialize_Scalar(ulong seed0, ulong seed1, ulong seed2, ulong seed3) {
			const int STEPS = 1;
			const int ROUNDS = 13;

			// Diffuse first two seed elements in s0, then the last two. Same for s1.
			// We must keep half of the state unchanged so users cannot set a bad state.
			PrngState state = default;
			for (int i = 0; i < 16; i++) state.scalar_state[i] = PHI[i];
			state.scalar_state[0 * 2 + 0] ^= seed0;
			state.scalar_state[0 * 2 + 8] ^= seed2;
			state.scalar_state[1 * 2 + 0] ^= seed1;
			state.scalar_state[1 * 2 + 8] ^= seed3;
			state.scalar_state[2 * 2 + 0] ^= seed2;
			state.scalar_state[2 * 2 + 8] ^= seed0;
			state.scalar_state[3 * 2 + 0] ^= seed3;
			state.scalar_state[3 * 2 + 8] ^= seed1;

			for (int i = 0; i < ROUNDS; i++) {
				Generate_Scalar(ref state, null, 128 * STEPS);
				for (int j = 0; j < 4; j++) {
					state.scalar_state[j + 00] = state.scalar_output[j + 12];
					state.scalar_state[j + 04] = state.scalar_output[j + 08];
					state.scalar_state[j + 08] = state.scalar_output[j + 04];
					state.scalar_state[j + 12] = state.scalar_output[j + 00];
				}
			}

			return state;
		}

		/// <summary>
		/// Writes <paramref name="val"/> to memory, storing it such that it is <em>always</em>
		/// little endian (even if the system is big endian)
		/// </summary>
		/// <param name="dst">The destination pointer to write the value at.</param>
		/// <param name="val">The value to write.</param>
		private static unsafe void WriteToSpanAlwaysLE(Span<byte> dst, ulong val) {
			if (BitConverter.IsLittleEndian) {
				BitConverter.TryWriteBytes(dst, val);
			} else {
				for (int i = 0; i < sizeof(ulong); i++) {
					dst[i] = (byte)(val & 0xFF);
					val >>= 8;
				}
			}
		}
	}
}