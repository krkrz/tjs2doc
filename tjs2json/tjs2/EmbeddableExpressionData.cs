using System;
using System.Collections.Generic;
using System.Text;

namespace TJS2 {
	sealed class EmbeddableExpressionData {
		public const int START = 0;
		public const int NEXT_IS_STRING_LITERAL = 1;
		public const int NEXT_IS_EXPRESSION = 2;

		public int mState;
		public int mWaitingNestLevel;
		//public int mWaitingToken;
		public int mDelimiter;
		public bool mNeedPlus;
	}
}
