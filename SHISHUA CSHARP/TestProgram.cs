#if !RELEASE
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace SHISHUADotNet {
	public class TestProgram {
		private static void WriteByteArray(Span<byte> buf) {
			for (int i = 0; i < buf.Length; i++) {
				Console.Write($"{buf[i]:X2}");
			}
			Console.WriteLine();
		}

		private static void SpeedTestOutput(SHISHUA instance) {
			Stopwatch sw = new Stopwatch();
			sw.Start();
			const ulong iterations = 100000000;
			const int size = 4096;
			const ulong bytes = iterations * size;
			for (ulong i = 0; i < iterations; i++) {
				instance.Generate(null, size);
			}
			sw.Stop();
			Console.WriteLine($"Generated {bytes} bytes of data in {sw.ElapsedTicks * 100.0D} nanos, or {sw.ElapsedMilliseconds / 1000.0D} seconds");
		}

		public static void Main() {
			SHISHUA instance = new SHISHUA(0xDEADBEEF, 0x69420, 0x123456789101112, 0x13371337);
			Span<byte> result = stackalloc byte[128];
			instance.Generate(result);
			WriteByteArray(result);
			SpeedTestOutput(instance);
		}
	}
}
#endif