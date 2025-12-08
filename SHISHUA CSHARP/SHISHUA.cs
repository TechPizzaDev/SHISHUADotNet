using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace SHISHUA_CSHARP {

	/// <summary>
	/// SHISHUA is the current world record holder (as of 2025) for the fastest pRNG algorithm ever created.
	/// On top of its unmatched speed, it also has no known instances of failing PractRand (32TiB of data and
	/// seed correlation tests checked), making it significantly better than its alternatives.
	/// <para/>
	/// This reimplementation supports supports AVX2, SSE2, and Scalar modes.
	/// It does not support the half-size implementations at this time, nor does it support ARM for lack of Neon
	/// support in C#.
	/// </summary>
	public partial class SHISHUA {

		

	}
}