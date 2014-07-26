using System;
using System.Collections.Generic;
using System.Text;

namespace TJS2 {
	class CompileException : Exception {
		public CompileException( string mes, int index ) : base(mes) {
			Index = index;
		}
		public int Index { set; get; }
	}
}
