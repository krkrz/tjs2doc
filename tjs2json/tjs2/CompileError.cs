using System;
using System.Collections.Generic;
using System.Text;

namespace TJS2 {
	sealed class CompileError {
		private int mStart;
		private int mEnd;
		private string mMessage;
		public CompileError( string mes, int start, int end ) {
			mMessage = mes;
			mStart = start;
			mEnd = end;
		}
		public string Message { get { return mMessage; } }
		public int Start { get { return mStart; } }
		public int End { get { return mEnd; } }
	}
}
