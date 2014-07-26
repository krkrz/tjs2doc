using System;
using System.Collections.Generic;
using System.Text;

namespace TJS2{
	sealed class VariantClosure {
		public object mObject;
		public object mObjThis;
		public VariantClosure( object obj ) {
			mObject = obj;
			mObjThis = null;
		}
	}
}
