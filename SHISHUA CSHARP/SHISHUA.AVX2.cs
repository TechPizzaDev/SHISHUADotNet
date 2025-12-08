using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace SHISHUA_CSHARP {
	partial class SHISHUA {

		/// <summary>
		/// The main routine of SHISHUA AVX2, translated to C#.
		/// </summary>
		/// <param name="state">The randomizer state.</param>
		/// <param name="resultBuffer">The output buffer to store generated random bytes into. Can be <see langword="null"/> to skip storing data and advance the state anyway.</param>
		/// <param name="generationSize">The amount of bytes to generate. If the <paramref name="resultBuffer"/> is not <see langword="null"/> (or, empty), this must match its size. Must be divisible by 128.</param>
		/// <exception cref="InvalidOperationException"></exception>
		private static void Generate_AVX2(ref PrngState state, Span<byte> resultBuffer, int generationSize) {
			if (!resultBuffer.IsEmpty) {
				if (resultBuffer.Length != generationSize) throw new ArgumentException($"The {nameof(generationSize)} parameter must be equal to {nameof(resultBuffer)}.Length");
				if (generationSize % 128 != 0) throw new ArgumentException($"The {nameof(generationSize)} parameter (and by extension {nameof(resultBuffer)}.Length) must be divisible by 128.");
			}
			Vector256<int> o0 = state.avx2_output0;
			Vector256<int> o1 = state.avx2_output1;
			Vector256<int> o2 = state.avx2_output2;
			Vector256<int> o3 = state.avx2_output3;
			Vector256<int> s0 = state.avx2_state0;
			Vector256<int> s1 = state.avx2_state1;
			Vector256<int> s2 = state.avx2_state2;
			Vector256<int> s3 = state.avx2_state3;
			Vector256<int> t0 = default;
			Vector256<int> t1 = default;
			Vector256<int> t2 = default;
			Vector256<int> t3 = default;
			Vector256<int> u0 = default;
			Vector256<int> u1 = default;
			Vector256<int> u2 = default;
			Vector256<int> u3 = default;
			Vector256<int> counter = state.avx2_counter;

			Vector256<int> shu0 = _mm256_set_epi32(4, 3, 2, 1, 0, 7, 6, 5);
			Vector256<int> shu1 = _mm256_set_epi32(2, 1, 0, 7, 6, 5, 4, 3);
			Vector256<int> increment = _mm256_set_epi64x(1UL, 3UL, 5UL, 7UL);

			for (int i = 0; i < generationSize; i += 128) {
				if (!resultBuffer.IsEmpty) {
					unsafe {
						fixed (byte* bufPtr = resultBuffer) {
							Avx.Store((int*)&bufPtr[i + 00], o0);
							Avx.Store((int*)&bufPtr[i + 32], o1);
							Avx.Store((int*)&bufPtr[i + 64], o2);
							Avx.Store((int*)&bufPtr[i + 96], o3);
						}
					}
				}

				s1 = _mm256_add_epi64(s1, counter);
				s3 = _mm256_add_epi64(s3, counter);
				counter = _mm256_add_epi64(counter, increment);

				u0 = _mm256_srli_epi64(s0, 1);
				u1 = _mm256_srli_epi64(s1, 3);
				u2 = _mm256_srli_epi64(s2, 1);
				u3 = _mm256_srli_epi64(s3, 3);
				t0 = Avx2.PermuteVar8x32(s0, shu0);
				t1 = Avx2.PermuteVar8x32(s1, shu1);
				t2 = Avx2.PermuteVar8x32(s2, shu0);
				t3 = Avx2.PermuteVar8x32(s3, shu1);

				s0 = _mm256_add_epi64(t0, u0);
				s1 = _mm256_add_epi64(t1, u1);
				s2 = _mm256_add_epi64(t2, u2);
				s3 = _mm256_add_epi64(t3, u3);

				o0 = Avx2.Xor(u0, t1);
				o1 = Avx2.Xor(u2, t3);
				o2 = Avx2.Xor(s0, s3);
				o3 = Avx2.Xor(s2, s1);
			}
			state.avx2_output0 = o0;
			state.avx2_output1 = o1;
			state.avx2_output2 = o2;
			state.avx2_output3 = o3;
			state.avx2_state0 = s0;
			state.avx2_state1 = s1;
			state.avx2_state2 = s2;
			state.avx2_state3 = s3;
			state.avx2_counter = counter;
		}

		/// <summary>
		/// Initializes the randomizer state using AVX2 logic.
		/// </summary>
		/// <param name="seed0">The first 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed1">The second 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed2">The third 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed3">The fourth 64 of 256 bits needed to create a seed.</param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">The CPU does not support AVX or AVX2.</exception>
		private static unsafe PrngState Initialize_AVX2(ulong seed0, ulong seed1, ulong seed2, ulong seed3) {
			const int STEPS = 1;
			const int ROUNDS = 13;

			if (!Avx2.IsSupported) throw new NotSupportedException("Missing CPU Feature: AVX2");
			if (!Avx.IsSupported) throw new NotSupportedException("Missing CPU Feature: AVX");
			PrngState state = default;
			state.avx2_state0 = _mm256_set_epi64x(PHI[3], PHI[2] ^ seed1, PHI[1], PHI[0] ^ seed0);
			state.avx2_state1 = _mm256_set_epi64x(PHI[7], PHI[6] ^ seed3, PHI[5], PHI[4] ^ seed2);
			state.avx2_state2 = _mm256_set_epi64x(PHI[11], PHI[10] ^ seed3, PHI[9], PHI[8] ^ seed2);
			state.avx2_state3 = _mm256_set_epi64x(PHI[15], PHI[14] ^ seed1, PHI[13], PHI[12] ^ seed0);
			for (int i = 0; i < ROUNDS; i++) {
				Generate_AVX2(ref state, null, 128 * STEPS);
				state.avx2_state0 = state.avx2_output3; state.avx2_state1 = state.avx2_output2;
				state.avx2_state2 = state.avx2_output1; state.avx2_state3 = state.avx2_output0;
			}
			return state;
		}

		/// <summary>
		/// The same as <see cref="Vector256.Create(ulong, ulong, ulong, ulong)"/> but with its parameters flipped
		/// to mimic <c>_mm256_set_epi64x</c> on a little endian system. This does not handle big endian at this time.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m256i is m256i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="a">The first pair of ints to store.</param>
		/// <param name="b">The second pair of ints to store.</param>
		/// <param name="c">The third pair of ints to store.</param>
		/// <param name="d">The fourth pair of ints to store.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<int> _mm256_set_epi64x(ulong a, ulong b, ulong c, ulong d) {
			// TODO: Big endian check?
			return Vector256.Create(d, c, b, a).AsInt32();
		}

		/// <summary>
		/// The same as <see cref="Vector256.Create(int, int, int, int, int, int, int, int)"/> but with its parameters flipped
		/// to mimic <c>_mm256_set_epi32</c> on a little endian system. This does not handle big endian at this time.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m256i is m256i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="a">The first int to store.</param>
		/// <param name="b">The second int to store.</param>
		/// <param name="c">The third int to store.</param>
		/// <param name="d">The fourth int to store.</param>
		/// <param name="e">The fifth int to store.</param>
		/// <param name="f">The sixth int to store.</param>
		/// <param name="g">The seventh int to store.</param>
		/// <param name="h">The eighth int to store.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<int> _mm256_set_epi32(int a, int b, int c, int d, int e, int f, int g, int h) {
			// TODO: Big endian check?
			return Vector256.Create(h, g, f, e, d, c, b, a);
		}

		/// <summary>
		/// The same as <see cref="Avx2.Add(Vector256{long}, Vector256{long})"/> but the parameters are accepted
		/// as <see cref="int"/> vectors instead of <see cref="long"/> vectors. Behavior is the same for <see cref="long"/>
		/// vectors.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m256i is m256i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="left">The first value to add.</param>
		/// <param name="right">The second value to add.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<int> _mm256_add_epi64(Vector256<int> left, Vector256<int> right) {
			return Avx2.Add(left.AsInt64(), right.AsInt64()).AsInt32();
		}

		/// <summary>
		/// The same as <see cref="Avx2.ShiftRightLogical(Vector256{long}, byte)"/> but the first parameter is
		/// accepted as an <see cref="int"/> vector rather than a <see cref="long"/> vector. Behavior is the same
		/// for a <see cref="long"/> vector.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m256i is m256i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="value">The value to shift.</param>
		/// <param name="shift">The amount to shift by.</param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<int> _mm256_srli_epi64(Vector256<int> value, [ConstantExpected] byte shift) {
			return Avx2.ShiftRightLogical(value.AsInt64(), shift).AsInt32();
		}


	}
}