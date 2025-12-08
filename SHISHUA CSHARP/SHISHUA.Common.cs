using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;

namespace SHISHUA_CSHARP {
	partial class SHISHUA {

		/// <summary>
		/// The digits of Phi are used during initialization.
		/// </summary>
		private static readonly ulong[] PHI = [
			0x9E3779B97F4A7C15, 0xF39CC0605CEDC834, 0x1082276BF3A27251, 0xF86C6A11D0C18E95,
			0x2767F0B153D27B7F, 0x0347045B5BF1827F, 0x01886F0928403002, 0xC1D64BA40F335E36,
			0xF06AD7AE9717877E, 0x85839D6EFFBD7DC6, 0x64D325D1C5371682, 0xCADD0CCCFDFFBBE1,
			0x626E33B8D04B4331, 0xBBF73C790D94F79D, 0x471C4AB3ED3D82A5, 0xFEC507705E4AE6E5
		];


		/// <summary>
		/// The pRNG state stores all values needed to iterate.
		/// </summary>
		[StructLayout(LayoutKind.Explicit, Pack = 4)]
		private unsafe struct PrngState {
			const int SIZEOF_I64 = 8;
			const int SIZEOF_M128I = 16;
			const int SIZEOF_M256I = 32;

			#region Scalar Implementation
			[FieldOffset(SIZEOF_I64 * 0)]
			public fixed ulong scalar_state[16];
			[FieldOffset(SIZEOF_I64 * 16)]
			public fixed ulong scalar_output[16];
			[FieldOffset(SIZEOF_I64 * 32)]
			public fixed ulong scalar_counter[4];
			#endregion

			// n.b. Did some testing. We can't use fixed size buffers of VectorN types.
			// Hacking together an inner struct to union a set of ulongs with vectors absolutely obliterates JIT.
			// And so, a wall of fields is the way to go, in the way of getting the best performance.

			#region SSE2 Implementation
			[FieldOffset(SIZEOF_M128I * 0)]
			public Vector128<int> sse2_state0;
			[FieldOffset(SIZEOF_M128I * 1)]
			public Vector128<int> sse2_state1;
			[FieldOffset(SIZEOF_M128I * 2)]
			public Vector128<int> sse2_state2;
			[FieldOffset(SIZEOF_M128I * 3)]
			public Vector128<int> sse2_state3;
			[FieldOffset(SIZEOF_M128I * 4)]
			public Vector128<int> sse2_state4;
			[FieldOffset(SIZEOF_M128I * 5)]
			public Vector128<int> sse2_state5;
			[FieldOffset(SIZEOF_M128I * 6)]
			public Vector128<int> sse2_state6;
			[FieldOffset(SIZEOF_M128I * 7)]
			public Vector128<int> sse2_state7;

			[FieldOffset(SIZEOF_M128I * 8)]
			public Vector128<int> sse2_output0;
			[FieldOffset(SIZEOF_M128I * 9)]
			public Vector128<int> sse2_output1;
			[FieldOffset(SIZEOF_M128I * 10)]
			public Vector128<int> sse2_output2;
			[FieldOffset(SIZEOF_M128I * 11)]
			public Vector128<int> sse2_output3;
			[FieldOffset(SIZEOF_M128I * 12)]
			public Vector128<int> sse2_output4;
			[FieldOffset(SIZEOF_M128I * 13)]
			public Vector128<int> sse2_output5;
			[FieldOffset(SIZEOF_M128I * 14)]
			public Vector128<int> sse2_output6;
			[FieldOffset(SIZEOF_M128I * 15)]
			public Vector128<int> sse2_output7;

			[FieldOffset(SIZEOF_M128I * 16)]
			public Vector128<int> sse2_counter0;
			[FieldOffset(SIZEOF_M128I * 17)]
			public Vector128<int> sse2_counter1;
			#endregion

			#region AVX2 Implementation
			[FieldOffset(SIZEOF_M256I * 0)]
			public Vector256<int> avx2_state0;
			[FieldOffset(SIZEOF_M256I * 1)]
			public Vector256<int> avx2_state1;
			[FieldOffset(SIZEOF_M256I * 2)]
			public Vector256<int> avx2_state2;
			[FieldOffset(SIZEOF_M256I * 3)]
			public Vector256<int> avx2_state3;

			[FieldOffset(SIZEOF_M256I * 4)]
			public Vector256<int> avx2_output0;
			[FieldOffset(SIZEOF_M256I * 5)]
			public Vector256<int> avx2_output1;
			[FieldOffset(SIZEOF_M256I * 6)]
			public Vector256<int> avx2_output2;
			[FieldOffset(SIZEOF_M256I * 7)]
			public Vector256<int> avx2_output3;

			[FieldOffset(SIZEOF_M256I * 8)]
			public Vector256<int> avx2_counter;
			#endregion
		}
	}
}