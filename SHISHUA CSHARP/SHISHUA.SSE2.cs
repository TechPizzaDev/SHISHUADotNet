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

		// SSE2 Implementation. All 64 bit processors support this.

		/// <summary>
		/// The main routine of SHISHUA SSE2, translated to C#.
		/// </summary>
		/// <param name="state">The randomizer state.</param>
		/// <param name="resultBuffer">The output buffer to store generated random bytes into. Can be <see langword="null"/> to skip storing data and advance the state anyway.</param>
		/// <param name="generationSize">The amount of bytes to generate. If the <paramref name="resultBuffer"/> is not <see langword="null"/> (or, empty), this must match its size. Must be divisible by 128.</param>
		private static void Generate_SSE2(ref PrngState state, Span<byte> resultBuffer, int generationSize) {
			if (!resultBuffer.IsEmpty) {
				if (resultBuffer.Length != generationSize) throw new ArgumentException($"The {nameof(generationSize)} parameter must be equal to {nameof(resultBuffer)}.Length");
				if (generationSize % 128 != 0) throw new ArgumentException($"The {nameof(generationSize)} parameter (and by extension {nameof(resultBuffer)}.Length) must be divisible by 128.");
			}
			Vector128<int> counter_lo = state.sse2_counter0;
			Vector128<int> counter_hi = state.sse2_counter1;
			Vector128<int> increment_lo = _mm_set_epi64x(5, 7);
			Vector128<int> increment_hi = _mm_set_epi64x(1, 3);

			for (int i = 0; i < generationSize; i += 128) {
				// Write the current output block to state if it is not NULL
				if (!resultBuffer.IsEmpty) {
					unsafe {
						fixed (byte* bufPtr = resultBuffer) {
							Sse2.Store((int*)&bufPtr[i + (16 * 0)], state.sse2_output0);
							Sse2.Store((int*)&bufPtr[i + (16 * 1)], state.sse2_output1);
							Sse2.Store((int*)&bufPtr[i + (16 * 2)], state.sse2_output2);
							Sse2.Store((int*)&bufPtr[i + (16 * 3)], state.sse2_output3);
							Sse2.Store((int*)&bufPtr[i + (16 * 4)], state.sse2_output4);
							Sse2.Store((int*)&bufPtr[i + (16 * 5)], state.sse2_output5);
							Sse2.Store((int*)&bufPtr[i + (16 * 6)], state.sse2_output6);
							Sse2.Store((int*)&bufPtr[i + (16 * 7)], state.sse2_output7);
						}
					}
				}

				// The original code used a loop that got unrolled by the compiler.
				// Because my state doesn't use arrays (on the count of the fact that C# does not like fixed
				// size buffers of non-primitive types), I have to do this little hack.
				// This has no significant detriment to performance and has similar characteristics.
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				static void _laneparse(ref PrngState s, bool lower, Vector128<int> counter_lo, Vector128<int> counter_hi) {
					Vector128<int> s_lo;
					Vector128<int> s_hi;
					Vector128<int> u0_lo;
					Vector128<int> u0_hi;
					Vector128<int> u1_lo;
					Vector128<int> u1_hi;
					Vector128<int> t_lo;
					Vector128<int> t_hi;

					// Lane 0
					s_lo = lower ? s.sse2_state0 : s.sse2_state4;
					s_hi = lower ? s.sse2_state1 : s.sse2_state5;
					u0_lo = _mm_srli_epi64(s_lo, 1);
					u0_hi = _mm_srli_epi64(s_hi, 1);
					t_lo = _mm_alignr_epi8_compat(s_lo, s_hi, 4);
					t_hi = _mm_alignr_epi8_compat(s_hi, s_lo, 4);
					if (lower) {
						s.sse2_state0 = _mm_add_epi64(t_lo, u0_lo);
						s.sse2_state1 = _mm_add_epi64(t_hi, u0_hi);
					} else {
						s.sse2_state4 = _mm_add_epi64(t_lo, u0_lo);
						s.sse2_state5 = _mm_add_epi64(t_hi, u0_hi);
					}


					// Lane 1
					s_lo = lower ? s.sse2_state2 : s.sse2_state6;
					s_hi = lower ? s.sse2_state3 : s.sse2_state7;
					s_lo = _mm_add_epi64(s_lo, counter_lo);
					s_hi = _mm_add_epi64(s_hi, counter_hi);
					u1_lo = _mm_srli_epi64(s_lo, 3);
					u1_hi = _mm_srli_epi64(s_hi, 3);
					t_lo = _mm_alignr_epi8_compat(s_hi, s_lo, 12);
					t_hi = _mm_alignr_epi8_compat(s_lo, s_hi, 12);
					if (lower) {
						s.sse2_state2 = _mm_add_epi64(t_lo, u1_lo);
						s.sse2_state3 = _mm_add_epi64(t_hi, u1_hi);
					} else {
						s.sse2_state6 = _mm_add_epi64(t_lo, u1_lo);
						s.sse2_state7 = _mm_add_epi64(t_hi, u1_hi);
					}

					// Merge the lanes, finally:
					if (lower) {
						s.sse2_output0 = Sse2.Xor(u0_lo, t_lo);
						s.sse2_output1 = Sse2.Xor(u0_hi, t_hi);
					} else {
						s.sse2_output2 = Sse2.Xor(u0_lo, t_lo);
						s.sse2_output3 = Sse2.Xor(u0_hi, t_hi);
					}
				}
				_laneparse(ref state, true, counter_lo, counter_hi);
				_laneparse(ref state, false, counter_lo, counter_hi);

				state.sse2_output4 = Sse2.Xor(state.sse2_state0, state.sse2_state6);
				state.sse2_output5 = Sse2.Xor(state.sse2_state1, state.sse2_state7);
				state.sse2_output6 = Sse2.Xor(state.sse2_state4, state.sse2_state2);
				state.sse2_output7 = Sse2.Xor(state.sse2_state5, state.sse2_state3);

				counter_lo = _mm_add_epi64(counter_lo, increment_lo);
				counter_hi = _mm_add_epi64(counter_hi, increment_hi);
			}

			state.sse2_counter0 = counter_lo;
			state.sse2_counter1 = counter_hi;
		}

		/// <summary>
		/// Initializes the randomizer state using SSE2 logic.
		/// </summary>
		/// <param name="seed0">The first 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed1">The second 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed2">The third 64 of 256 bits needed to create a seed.</param>
		/// <param name="seed3">The fourth 64 of 256 bits needed to create a seed.</param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException">The CPU does not support SSE2.</exception>
		private static unsafe PrngState Initialize_SSE2(ulong seed0, ulong seed1, ulong seed2, ulong seed3) {
			const int STEPS = 1;
			const int ROUNDS = 13;

			if (!Sse2.IsSupported) throw new NotSupportedException("Missing CPU Feature: SSE2.");
			PrngState state = default;

			Vector128<int> seed_0 = _mm_cvtsi64_si128(seed0);
			Vector128<int> seed_1 = _mm_cvtsi64_si128(seed1);
			Vector128<int> seed_2 = _mm_cvtsi64_si128(seed2);
			Vector128<int> seed_3 = _mm_cvtsi64_si128(seed3);
			fixed (ulong* phi = &PHI[0]) {
				state.sse2_state0 = Sse2.Xor(seed_0, Sse2.LoadVector128((int*)&phi[0]));
				state.sse2_state1 = Sse2.Xor(seed_1, Sse2.LoadVector128((int*)&phi[2]));
				state.sse2_state2 = Sse2.Xor(seed_2, Sse2.LoadVector128((int*)&phi[4]));
				state.sse2_state3 = Sse2.Xor(seed_3, Sse2.LoadVector128((int*)&phi[6]));
				state.sse2_state4 = Sse2.Xor(seed_2, Sse2.LoadVector128((int*)&phi[8]));
				state.sse2_state5 = Sse2.Xor(seed_3, Sse2.LoadVector128((int*)&phi[10]));
				state.sse2_state6 = Sse2.Xor(seed_0, Sse2.LoadVector128((int*)&phi[12]));
				state.sse2_state7 = Sse2.Xor(seed_1, Sse2.LoadVector128((int*)&phi[14]));
			}

			for (int i = 0; i < ROUNDS; i++) {
				Generate_SSE2(ref state, null, 128 * STEPS);
				state.sse2_state0 = state.sse2_output6; state.sse2_state1 = state.sse2_output7;
				state.sse2_state2 = state.sse2_output4; state.sse2_state3 = state.sse2_output5;
				state.sse2_state4 = state.sse2_output2; state.sse2_state5 = state.sse2_output3;
				state.sse2_state6 = state.sse2_output0; state.sse2_state7 = state.sse2_output1;
			}
			return state;
		}


		/// <summary>
		/// Directly uses <see cref="Ssse3.AlignRight(Vector128{int}, Vector128{int}, byte)"/> unless SSSE3 is not supported,
		/// in which case an emulation using stock SSE2 is implemented.
		/// </summary>
		/// <param name="hi">The high operand contains the most significant bytes.</param>
		/// <param name="lo">The low operand contains the most significant bytes.</param>
		/// <param name="mask">The mask parameter of the alignment method.</param>
		/// <returns></returns>
#pragma warning disable IDE0079 // Because for some reason, VS thinks the suppression below doesn't do anything?
#pragma warning disable CA1857 // The caller of this method should pass a constant, and then the non-constant SSE2 fallback will thus only have one possible form.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector128<int> _mm_alignr_epi8_compat(Vector128<int> hi, Vector128<int> lo, [ConstantExpected] byte mask) {
			if (Ssse3.IsSupported) {
				return Ssse3.AlignRight(hi, lo, mask);
			} else {
				// A bit slower.
				return Sse2.Or(
					Sse2.ShiftLeftLogical128BitLane(hi, (byte)(16 - mask)),
					Sse2.ShiftLeftLogical128BitLane(lo, mask)
				);
			}
		}
#pragma warning restore CA1857
#pragma warning restore IDE0079

		/// <summary>
		/// The same as <see cref="Sse2.ShiftRightLogical(Vector128{long}, byte)"/>, but it accepts
		/// an <see cref="int"/> vector.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m128i is m128i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="a">The vector to shift.</param>
		/// <param name="imm8">The value used to shift.</param>
		/// <returns></returns>
		private static Vector128<int> _mm_srli_epi64(Vector128<int> a, [ConstantExpected] byte imm8) {
			return Sse2.ShiftRightLogical(a.AsInt64(), imm8).AsInt32();
		}

		/// <summary>
		/// The same as <see cref="Sse2.Add(Vector128{long}, Vector128{long})"/> but the operands are
		/// <see cref="int"/> vectors.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m128i is m128i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="a">The left operand to add.</param>
		/// <param name="b">The right operand to add.</param>
		/// <returns></returns>
		private static Vector128<int> _mm_add_epi64(Vector128<int> a, Vector128<int> b) {
			return Sse2.Add(a.AsInt64(), b.AsInt64()).AsInt32();
		}

		/// <summary>
		/// The same as <see cref="Vector128.Create(long, long)"/> but with the operands reversed to mimic
		/// <c>_mm_set_epi64x</c>. Also returns as an <see cref="int"/> vector.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m128i is m128i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="a">The high bits to store.</param>
		/// <param name="b">The low bits to store.</param>
		/// <returns></returns>
		private static Vector128<int> _mm_set_epi64x(long a, long b) {
			return Vector128.Create(b, a).AsInt32();
		}

		/// <summary>
		/// The same as <see cref="Vector128.Create(long, long)"/>, with a 0 as its first argument, to mimic
		/// <c>_mm_cvtsi64_si128</c>. Returns an <see cref="int"/> vector.
		/// </summary>
		/// <remarks>
		/// This is a compatibility method because the native C++ libraries are not strictly typed; m128i is m128i.
		/// C# has no such analogue and so hacks like this are needed to get around restrictions of the generic type.
		/// </remarks>
		/// <param name="a">The vlaue to store.</param>
		/// <returns></returns>
		private static Vector128<int> _mm_cvtsi64_si128(ulong a) {
			return Vector128.Create(a, 0).AsInt32();
		}

	}
}