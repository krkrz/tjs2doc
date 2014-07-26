using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace TJS2 {
	sealed class ScriptLineData {
		private readonly string mString;

		private ArrayList mLineVector;
		private ArrayList mLineLengthVector;
		private int mLineOffset;

		private const int CARRIAGE_RETURN = 13;
		private const int LINE_FEED = 10;
		
		public ScriptLineData( string strval, int offset ) {
			mString = strval;
			mLineOffset = offset;
		}
		
		private void generateLineVector() {
			mLineVector = new ArrayList();
			mLineLengthVector = new ArrayList();
			int count = mString.Length;
			int lastCR = 0;
			int i;
			for( i= 0; i < count; i++ ) {
				int c = mString[i];
				if( c == CARRIAGE_RETURN || c == LINE_FEED ) {
					mLineVector.Add( lastCR );
					mLineLengthVector.Add( i-lastCR );
					lastCR = i+1;
					if( (i+1) < count ) {
						c = mString[i+1];
						if( c == CARRIAGE_RETURN || c == LINE_FEED ) {
							i++;
							lastCR = i+1;
						}
					}
				}
			}
			if( i != lastCR ) {
				mLineVector.Add( lastCR );
				mLineLengthVector.Add( i-lastCR );
			}
		}
		public int getSrcPosToLine( int pos ) {
			if( mLineVector == null ) {
				generateLineVector();
			}
			// 2分法によって位置を求める
			int s = 0;
			int e = mLineVector.Count;
			while( true ) {
				if( (e-s) <= 1 ) return s + mLineOffset;
				int m = s + (e-s)/2;
				if( (int)mLineVector[m] > pos )
					e = m;
				else
					s = m;
			}
		}
		public int getLineToSrcPos( int pos ) {
			if( mLineVector == null ) {
				generateLineVector();
			}
			return (int)mLineVector[pos];
		}
		public string getLine( int line ) {
			if( mLineVector == null ) {
				generateLineVector();
			}
			int start = (int)mLineVector[line];
			int length = (int)mLineLengthVector[line];
			return mString.Substring(start, length );
		}
		public int getMaxLine() {
			if( mLineVector == null ) {
				generateLineVector();
			}
			return mLineVector.Count;
		}
	}
}
