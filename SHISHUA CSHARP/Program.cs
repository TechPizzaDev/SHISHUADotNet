
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SHISHUA_CSHARP {
	partial class SHISHUA {
		// AVX2 IMPLEMENTATION

		private static void WriteByteArray(Span<byte> buf) {
			for (int i = 0; i < buf.Length; i++) {
				Console.Write($"{buf[i]:X2}");
			}
			Console.WriteLine();
		}

		private static void TestOutput() {
			Span<byte> bufA = stackalloc byte[256];
			Span<byte> bufB = stackalloc byte[256];
			Span<byte> bufC = stackalloc byte[256];
			PrngState state = Initialize_Scalar(0x123456789101112, 0xB00B135, 0x1337D1CC_00000000, 0x69420);
			Generate_Scalar(ref state, bufA, 256);
			state = Initialize_SSE2(0x123456789101112, 0xB00B135, 0x1337D1CC_00000000, 0x69420);
			Generate_SSE2(ref state, bufB, 256);
			state = Initialize_AVX2(0x123456789101112, 0xB00B135, 0x1337D1CC_00000000, 0x69420);
			Generate_AVX2(ref state, bufC, 256);

			bool aAndB = bufA.SequenceEqual(bufB);
			bool bAndC = bufB.SequenceEqual(bufC);
			if (aAndB && bAndC) {
				Console.WriteLine("Success!");
			} else {
				Console.WriteLine("Scalar:");
				WriteByteArray(bufA);
				Console.WriteLine();

				Console.WriteLine("SSE2:");
				WriteByteArray(bufB);
				Console.WriteLine();

				Console.WriteLine("AVX2:");
				WriteByteArray(bufC);
				Console.WriteLine();
			}
		}

		private static void SpeedTestOutput(ref PrngState state, int technique) {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			const ulong iterations = 100000000;
			const int size = 4096;
			const ulong bytes = iterations * size;
			if (technique == 0) {
				for (ulong i = 0; i < iterations; i++) {
					Generate_Scalar(ref state, null, size);
				}
			} else if (technique == 1) {
				for (ulong i = 0; i < iterations; i++) {
					Generate_SSE2(ref state, null, size);
				}
			} else if (technique == 2) {
				for (ulong i = 0; i < iterations; i++) {
					Generate_AVX2(ref state, null, size);
				}
			}
			sw.Stop();
			Console.WriteLine($"Generated {ToLargestUnitSize(bytes)} of data in {sw.ElapsedTicks * 100.0D} nanos, or {sw.ElapsedMilliseconds / 1000.0D} seconds");
		}

		public static unsafe void Main() {
			TestOutput();
		}

		public static string ToLargestUnitSize(ulong bytes) {
			string bytesText = "\"erm akchually isnt 1kB 1024 bytes?\" no it isnt dummy thats 1 kibibyte (kiB) not 1 kilobyte (kB)";
			if (bytes > 1_000_000_000_000) {
				bytesText = $"{(bytes / 1_000_000_000_000D):0.###} TB";
			} else if (bytes > 1_000_000_000) {
				bytesText = $"{(bytes / 1_000_000_000D):0.###} GB";
			} else if (bytes > 1_000_000) {
				bytesText = $"{(bytes / 1_000_000D):0.###} MB";
			} else if (bytes > 1_000) {
				bytesText = $"{(bytes / 1_000D):0.###} kB";
			} else {
				bytesText = $"{bytes:N0} Bytes";
			}
			return bytesText;
		}
	}
}
