using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace TJS2 {
	sealed class LexicalAnalyzer {
		private bool mIsFirst;
		private bool mIsExprMode;
		private bool mResultNeeded;
		private bool mRegularExpression;
		private bool mBareWord;
		//private bool mDicFunc;	// dicfunc quick-hack

		private int mValue;
		private int mPrevToken;
		private int mPrevPos;
		private int mNestLevel;
		private int mIfLevel;
		
		private const int BUFFER_CAPACITY = 1024;
		private const int CR = 13;
		private const int LF = 10;
		private const int TAB = 0x09;
		private const int SPACE = 0x20;

		private const int NOT_COMMENT = 0;
		private const int CONTINUE = 1;
		private const int ENDED = 2;

		// String Status
		private const int NONE = 0;
		private const int DELIMITER = 1;
		private const int AMPERSAND = 2;
		private const int DOLLAR = 3;

		private StringBuilder mWorkBuilder;
		private Queue<long> mRetValDeque;
		private ArrayList mValues;
		private ArrayList mEmbeddableExpressionDataStack;

		private char[] mText;
		private int mCurrent;
		private int mStringStatus;

		public object getValue( int idx ) { return mValues[idx];  }
		public string getString( int idx ) {
			object ret = mValues[idx];
			if( ret is string ) {
				return (string)ret;
			} else {
				return null;
			}
		}
		private int putValue( object val ) {
			mValues.Add( val );
			return mValues.Count - 1;
		}

		public LexicalAnalyzer( string script, bool isexpr, bool resultneeded ) {
			mWorkBuilder = new StringBuilder( BUFFER_CAPACITY );
			mRetValDeque = new Queue<long>();
			mValues = new ArrayList();
			mEmbeddableExpressionDataStack = new ArrayList();
			mIsExprMode = isexpr;
			mResultNeeded = resultneeded;
			mPrevToken = -1;
			int scriptLen = script.Length;
			char[] tmp = script.ToCharArray();
			if( mIsExprMode ) {
				mText = new char[scriptLen+2];
				Array.Copy( tmp, mText, scriptLen );
				mText[scriptLen] = ';';
				mText[scriptLen+1] = '\0';
			} else {
				mText = new char[scriptLen + 1];
				Array.Copy( tmp, mText, scriptLen );
				if( script.StartsWith( "#!" ) ) {
					mText[0] = mText[1] = '/';
				}
				mText[scriptLen] = '\0';
			}
			//----- dicfunc quick-hack はなし
			mIfLevel = 0;
			mPrevPos = 0;
			mNestLevel = 0;
			mIsFirst = true;
			mRegularExpression = false;
			mBareWord = false;
			putValue( null );
		}
		private int mStartComment = 0;
		private int mEndComment = 0;
		public string getComment() {
			return new string( mText, mStartComment, mEndComment - mStartComment );
		}
		private int parseComment() {
			char[] ptr = mText;
			int cur = mCurrent;
			if( ptr[cur] != '/' ) return NOT_COMMENT;
			char c = ptr[cur+1];
			if( c == '/' ) {
				// line comment
				cur += 2;
				mStartComment = cur;
				c = ptr[cur];
				while( (c != 0) && (c != CR && c != LF ) ) { cur++; c = ptr[cur]; }
				if( c != 0 && c == CR ) {
					cur++; c = ptr[cur];
					if( c == LF ) {
						cur++; c = ptr[cur];
					}
				}
				mEndComment = cur;
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				mCurrent = cur;
				if( c == 0 ) return ENDED;
				return CONTINUE;
			} else if( c == '*' ) {
				// ブロックコメント
				cur += 2;
				mStartComment = cur;
				c = ptr[cur];
				if( c == 0 ) {
					mCurrent = cur;
					throw new CompileException( Error.UnclosedComment, cur );
				}

				int level = 0;
				while(true) {
					if( c == '/' && ptr[cur+1] == '*' ) {
						// コメントのネスト
						level++;
					} else if( c == '*' && ptr[cur+1] == '/' ) {
						if( level == 0 ) {
							mEndComment = cur;
							cur += 2;
							c = ptr[cur];
							break;
						}
						level--;
					}
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) {
						mCurrent = cur;
						throw new CompileException( Error.UnclosedComment, cur );
					}
				}
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				mCurrent = cur;
				if( c == 0 ) return ENDED;
				return CONTINUE;
			}
			return NOT_COMMENT;
		}
		private const int TJS_IEEE_D_SIGNIFICAND_BITS = 52;
		private const int TJS_IEEE_D_EXP_MIN = -1022;
		private const int TJS_IEEE_D_EXP_MAX = 1023;
		private const long TJS_IEEE_D_EXP_BIAS = 1023;
		private bool parseExtractNumber( int basebits ) {
			bool point_found = false;
			bool exp_found = false;
			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( c != 0 ) {
				if( c == '.' && point_found == false && exp_found == false ) {
					point_found = true;
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				} else if( (c == 'p' || c == 'P') && exp_found == false ) {
					exp_found = true;
					cur++; c = ptr[cur];
					while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
					if( c == '+' || c == '-' ) {
						cur++; c = ptr[cur];
						while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
					}
				} else if( (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') ) {
					if( basebits == 3 ) {
						if( c < '0' || c > '7') break;
					} else if( basebits == 1 ) {
						if( c != '0' && c != '1') break;
					}
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				} else {
					break;
				}
			}
			return point_found || exp_found;
		}

		// base
		// 16進数 : 4
		// 2進数 : 1
		// 8進数 : 3
		private double parseNonDecimalReal( bool sign, int basebits ) {
			ulong main = 0;
			int exp = 0;
			int numsignif = 0;
			bool pointpassed = false;

			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( c != 0 ){
				if( c == '.' ) {
					pointpassed = true;
				} else if( c == 'p' || c == 'P' ) {
					cur++; c = ptr[cur];
					while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }

					bool biassign = false;
					if( c == '+' ) {
						biassign = false;
						cur++; c = ptr[cur];
						while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
					}

					if( c == '-' ) {
						biassign = true;
						cur++; c = ptr[cur];
						while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
					}

					int bias = 0;
					while( c >= '0' && c <= '9' ) {
						bias *= 10;
						bias += c - '0';
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					}
					if( biassign ) bias = -bias;
					exp += bias;
					break;
				} else {
					int n = -1;
					if( basebits == 4 ) {
						if(c >= '0' && c <= '9') n = c - '0';
						else if(c >= 'a' && c <= 'f') n = c - 'a' + 10;
						else if(c >= 'A' && c <= 'F') n = c - 'A' + 10;
						else break;
					} else if( basebits == 3 ) {
						if(c >= '0' && c <= '7') n = c - '0';
						else break;
					} else if( basebits == 1 ) {
						if(c == '0' || c == '1') n = c - '0';
						else break;
					}

					if( numsignif == 0 ) {
						int b = basebits - 1;
						while( b >= 0 ) {
							if( ((1<<b) & n) != 0 ) break;
							b--;
						}
						b++;
						if( b != 0 ) {
							// n is not zero
							numsignif = b;
							main |= ((ulong)n << (64-numsignif));
							if( pointpassed )
								exp -= (basebits - b + 1);
							else
								exp = b - 1;
						} else {
							// n is zero
							if( pointpassed ) exp -= basebits;
						}
					} else {
						// append to main
						if( (numsignif + basebits) < 64 ) {
							numsignif += basebits;
							main |= ((ulong)n << (64-numsignif));
						}
						if( pointpassed == false ) exp += basebits;
					}
				}
				cur++; c = ptr[cur];
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
			}
			mCurrent = cur;
			main >>= (64 - 1 - TJS_IEEE_D_SIGNIFICAND_BITS);
			if( main == 0 ) {
				return 0.0;
			}
			main &= ((1L << TJS_IEEE_D_SIGNIFICAND_BITS) - 1L);
			if( exp < TJS_IEEE_D_EXP_MIN ) {
				return 0.0;
			}
			if( exp > TJS_IEEE_D_EXP_MAX ) {
				if( sign ) {
					return Double.NegativeInfinity;
				} else {
					return Double.PositiveInfinity;
				}
			}
			// compose IEEE double
			//double d = Double.longBitsToDouble(0x8000000000000000L | ((exp + TJS_IEEE_D_EXP_BIAS) << 52) | main);
			ulong value = (((ulong)exp + TJS_IEEE_D_EXP_BIAS) << 52) | main;
			byte[] bytes = BitConverter.GetBytes(value);
			double d = BitConverter.ToDouble( bytes, 0 );
			if( sign ) d = -d;
			return d;
		}
		private int parseNonDecimalInteger16( bool sign ) {
			long v = 0;
			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( c != 0 ) {
				int n = -1;
				if(c >= '0' && c <= '9') n = c - '0';
				else if(c >= 'a' && c <= 'f') n = c - 'a' + 10;
				else if(c >= 'A' && c <= 'F') n = c - 'A' + 10;
				else break;
				v <<= 4;
				v += n;
				cur++; c = ptr[cur];
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
			}
			mCurrent = cur;
			if( sign ) {
				return (int)-v;
			} else {
				return (int)v;
			}
		}
		private int parseNonDecimalInteger8( bool sign ) {
			long v = 0;
			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( c != 0 ) {
				int n = -1;
				if(c >= '0' && c <= '7') n = c - '0';
				else break;
				v <<= 3;
				v += n;
				cur++; c = ptr[cur];
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
			}
			mCurrent = cur;
			if( sign ) {
				return (int)-v;
			} else {
				return (int)v;
			}
		}
		private int parseNonDecimalInteger2( bool sign ) {
			long v = 0;
			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( c != 0 ) {
				if( c == '1' ) {
					v <<= 1;
					v++;
				} else if( c == '0' ) {
					v <<= 1;
				} else {
					break;
				}
				//next();
				cur++; c = ptr[cur];
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
			}
			mCurrent = cur;
			if( sign ) {
				return (int)-v;
			} else {
				return (int)v;
			}
		}
		public class Number {
			public double dvalue;
			public long lvalue;
			public int type;

			public const int TYPE_NULL = 0;
			public const int TYPE_INT = 1;
			public const int TYPE_DOUBLE = 2;

			public Number( double v ) {
				dvalue = v;
				type = TYPE_DOUBLE;
			}
			public Number( long l ) {
				lvalue = l;
				type = TYPE_INT;
			}
			public Number() {
				type = TYPE_NULL;
			}
		};
		private Number parseNonDecimalNumber( bool sign, int basevalue ) {
			bool is_real = parseExtractNumber( basevalue );
			if( is_real ) {
				return new Number( parseNonDecimalReal( sign, basevalue ) );
			} else {
				switch( basevalue ) {
				case 4: return new Number( parseNonDecimalInteger16(sign) );
				case 3: return new Number( parseNonDecimalInteger8(sign) );
				case 1: return new Number( parseNonDecimalInteger2( sign ) );
				}
			}
			return null;
		}
		// @return : Integer or Double or null
		private Number parseNumber() {
			int num = 0;
			bool sign = false;
			bool skipNum = false;

			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			if( c == '+' ) {
				sign = false;
				cur++; c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				if( c == 0 ) {
					mCurrent = cur;
					return null;
				}
			} else if( c == '-' ) {
				sign = true;
				cur++; c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				if( c == 0 ) {
					mCurrent = cur;
					return null;
				}
			}

			if( c > '9' ) { // 't', 'f', 'N', 'I' は '9' より大きい
				if( c == 't' && ptr[cur+1] == 'r' && ptr[cur+2] == 'u' && ptr[cur+3] == 'e' ) {
					cur += 4;
					mCurrent = cur;
					return new Number(1);
				} else if( c == 'f' && ptr[cur+1] == 'a' && ptr[cur+2] == 'l' && ptr[cur+3] == 's' && ptr[cur+4] == 'e' ) {
					cur += 5;
					mCurrent = cur;
					return new Number(0);
				} else if( c == 'N' && ptr[cur+1] == 'a' && ptr[cur+2] == 'N' ) {
					cur += 3;
					mCurrent = cur;
					return new Number( Double.NaN );
				} else if( c == 'I' && ptr[cur+1] == 'n' && ptr[cur+2] == 'f' && ptr[cur+3] == 'i' && ptr[cur+4] == 'n' &&
						ptr[cur+5] == 'i' && ptr[cur+6] == 't' && ptr[cur+7] == 'y' ) {
					cur += 8;
					mCurrent = cur;
					if( sign ) {
						return new Number(Double.NegativeInfinity);
					} else {
						return new Number(Double.PositiveInfinity);
					}
				}
			}

			// 10進数以外か調べる
			if( c == '0' ) {
				cur++; c = ptr[cur];
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				if( c == 0 ) {
					mCurrent = cur;
					return new Number(0);
				}
				if( c == 'x' || c == 'X' ) {
					// hexadecimal
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					mCurrent = cur;
					if( c == 0 ) return null;
					return parseNonDecimalNumber(sign,4);
				} else if( c == 'b' || c == 'B' ) {
					// binary
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					mCurrent = cur;
					if( c == 0 ) return null;
					return parseNonDecimalNumber(sign,1);
				} else if( c == '.' ) {
					skipNum = true;
				} else if( c == 'e' || c == 'E' ) {
					skipNum = true;
				} else if( c == 'p' || c == 'P' ) {
					// 2^n exp
					mCurrent = cur;
					return null;
				} else if( c >= '0' && c <= '7' ) {
					// octal
					mCurrent = cur;
					return parseNonDecimalNumber(sign,3);
				}
			}

			if( skipNum == false ) {
				while( c != 0 ) {
					if( c < '0' || c > '9' ) break;
					num = num * 10 + ( c - '0' );
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				}
			}
			if( c == '.' || c == 'e' || c == 'E' ) {
				double figure = 1.0;
				int decimalv = 0;
				if( c == '.' ) {
					do {
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c < '0' || c > '9' ) break;
						decimalv = decimalv * 10 + ( c - '0' );
						figure *= 10;
					} while( c != 0 );
				}
				bool expSign = false;
				int expValue = 0;
				if( c == 'e' || c == 'E' ) {
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == '-' ) {
						expSign = true;
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					}

					while( c != 0 ) {
						if( c < '0' || c > '9' ) break;
						expValue = expValue * 10 + ( c - '0' );
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					}
				}
				double number = (double)num + ( (double)decimalv / figure );
				if( expValue != 0 ) {
					if( expSign == false ) {
						number *= Math.Pow( 10, expValue );
					} else {
						number /= Math.Pow( 10, expValue );
					}
				}
				if( sign ) number = -number;
				mCurrent = cur;
				return new Number( number );
			} else {
				if( sign ) num = -num;
				mCurrent = cur;
				return new Number( num );
			}
		}
		private string readString( int delimiter, bool embexpmode ) {
			mStringStatus = NONE;
			int cur = mCurrent;
			char[] ptr = mText;

			StringBuilder str = mWorkBuilder;
			str.Remove( 0, str.Length );

			while( ptr[cur] != 0 ) {
				char c = ptr[cur];
				if( c == '\\' ) {
					// escape
					// Next
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) break;

					if( c == 'x' || c == 'X' ) {	// hex
						// starts with a "\x", be parsed while characters are
						// recognized as hex-characters, but limited of size of char.
						// on Windows, \xXXXXX will be parsed to UNICODE 16bit characters.

						// Next
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c == 0 ) break;

						int code = 0;
						int count = 0;
						while( count < 4 ) {
							int n = -1;
							if(c >= '0' && c <= '9') n = c - '0';
							else if(c >= 'a' && c <= 'f') n = c - 'a' + 10;
							else if(c >= 'A' && c <= 'F') n = c - 'A' + 10;
							else break;

							code <<= 4; // *16
							code += n;
							count++;

							// Next
							cur++; c = ptr[cur];
							if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
							if( c == 0 ) break;
						}
						if( c == 0 ) break;
						str.Append( (char)code );
					} else if( c == '0' ) {	// octal
						// Next
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c == 0 ) break;

						int code = 0;
						while( true ) {
							int n = -1;
							if( c >= '0' && c <= '7' ) n = c - '0';
							else break;
							code <<= 3; // * 8
							code += n;

							// Next
							cur++; c = ptr[cur];
							if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
							if( c == 0 ) break;
						}
						str.Append( (char)code );
					} else {
						//str.append( (char)unescapeBackSlash(c) );
						switch(c) {
							case 'a': c = (char)0x07; break;
							case 'b': c = (char)0x08; break;
							case 'f': c = (char)0x0c; break;
							case 'n': c = (char)0x0a; break;
							case 'r': c = (char)0x0d; break;
							case 't': c = (char)0x09; break;
							case 'v': c = (char)0x0b; break;
						}
						str.Append( (char)c );

						// Next
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					}
				} else if( c == delimiter ) {
					// Next
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) {
						mStringStatus = DELIMITER;
						break;
					}

					int offset = cur;
					while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
					if( c == delimiter ) {
						// sequence of 'A' 'B' will be combined as 'AB'
						// Next
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					} else {
						cur = offset;
						mStringStatus = DELIMITER;
						break;
					}
				} else if( embexpmode == true && c == '&' ) {
					// Next
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) break;
					mStringStatus = AMPERSAND;
					break;
				} else if( embexpmode == true && c == '$' ) {
					// '{' must be placed immediately after '$'
					int offset = cur;
					// Next
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) break;

					if( c == '{' ) {
						// Next
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c == 0 ) break;
						mStringStatus = DOLLAR;
						break;
					} else {
						cur = offset;
						c = ptr[cur];
						str.Append( (char)c );
						// Next
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					}
				} else {
					str.Append( (char)c );
					// Next
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				}
			}
			mCurrent = cur;
			if( mStringStatus == NONE ) throw new CompileException(Error.StringParseError, mCurrent);
			return str.ToString();
		}
		public void setStartOfRegExp() { mRegularExpression = true; }
		public void setNextIsBareWord() { mBareWord = true; }
		private String parseRegExp() {
			bool ok = false;
			bool lastbackslash = false;
			StringBuilder str =  mWorkBuilder;
			str.Remove( 0, str.Length );
			StringBuilder flag = null;
			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( c != 0 ) {
				if( c == '\\' ) {
					str.Append( (char)c );
					if( lastbackslash == true )
						lastbackslash = false;
					else
						lastbackslash = true;
				} else if( c == '/' && lastbackslash == false ) {
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) {
						ok = true;
						break;
					}
					if( flag == null ) {
						flag = new StringBuilder(BUFFER_CAPACITY);
					} else {
						flag.Remove( 0, flag.Length );
					}
					while( c >= 'a' && c <= 'z' ) {
						flag.Append( (char)c );
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c == 0 ) break;
					}
					str.Insert( 0, "//" );
					String flgStr = flag.ToString();
					str.Insert( 2, flgStr );
					str.Insert( 2+flgStr.Length, "/" );
					ok = true;
					break;
				} else {
					lastbackslash = false;
					str.Append( (char)c );
				}
				cur++; c = ptr[cur];
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
			}
			int exceptionIndex = mCurrent;
			mCurrent = cur;
			if( !ok ) throw new CompileException( Error.RegexParseError, exceptionIndex );
			return str.ToString();
		}
		// 渡されたByteBufferを切り詰めた、新しいByteBufferを作る
		/*
		private MemoryStream compactByteBuffer( MemoryStream b ) {
			int count = b.position();
			ByteBuffer ret = ByteBuffer.allocate( count );
			b.position( 0 );
			for( int i = 0; i < count; i++ ) {
				ret.put( b.get() );
			}
			ret.position( 0 );
			return ret;
		}
		*/
		private MemoryStream parseOctet() {
			// parse a octet literal;
			// syntax is:
			// <% xx xx xx xx xx xx ... %>
			// where xx is hexadecimal 8bit(octet) binary representation.
			char[] ptr = mText;
			int cur = mCurrent + 1;
			char c = ptr[cur]; // skip %
			if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }

			bool leading = true;
			byte oct = 0;

			int count = 0;
			if( c != 0 ) {
				int offset = cur;
				int len = ptr.Length;
				while( (offset+1) < len ) {
					if( ptr[offset] == '%' && ptr[offset+1] == '>' ) break;
					offset++;
				}
				count = offset - cur;
			}
			count = count / 2 + 1;

			MemoryStream buffer = new MemoryStream( count );
			while( c != 0 ) {
				if( c == '/' ) {
					if( parseComment() == ENDED ) {
						mCurrent = cur;
						throw new CompileException( Error.OctetParseError, cur );
					}
				}

				c = ptr[cur];
				int n = cur+1;
				int next = ptr[n];
				if( next == CR && ptr[n+1] == LF ) { n++; c = ptr[n]; }
				if( c == '%' && next == '>' ) {
					cur = n;
					if( ptr[cur] != 0 ) {
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					}

					if( !leading ) {
						buffer.WriteByte( oct );
					}
					mCurrent = cur;
					//return compactByteBuffer(buffer);
					return buffer;
				}
				int num;
				if( c >= '0' && c <= '9' ) num = c - '0';
				else if( c >= 'a' && c <= 'f' ) num = c - 'a' + 10;
				else if( c >= 'A' && c <= 'F' ) num = c - 'A' + 10;
				else num = -1;
				if( num != -1 ) {
					if( leading ) {
						oct = (byte)num;
						leading = false;
					} else {
						oct <<= 4;
						oct += (byte)num;
						buffer.WriteByte( oct );
						leading = true;
					}
				}
				if( leading == false && c == ',' ) {
					buffer.WriteByte( oct );
					leading = true;
				}
				cur = n;
			}
			mCurrent = cur;
			throw new CompileException( Error.OctetParseError, cur );
		}
		
		private int parsePPExpression( String script ) {
			/*
			PreprocessorExpressionParser parser = new PreprocessorExpressionParser( mBlock.getTJS(), script );
			return parser.parse();
			*/
			return 1;
		}
		private int processPPStatement() {
			// process pre-prosessor statements.
			// int offset = mCurrent;
			// mCurrent++; // skip '@'
			int cur = mCurrent + 1; // skip '@'
			char[] ptr = mText;
			char c = ptr[cur];
			if( c == 's' && ptr[cur+1] == 'e' && ptr[cur+2] == 't' ) {
				// set statemenet
				// mBlock.notifyUsingPreProcessror();
				cur+=3;
				c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				if( c == 0 ) {
					mCurrent = cur;
					throw new CompileException( Error.PPError, cur );
				}
				if( c != '(' ) {
					mCurrent = cur;
					throw new CompileException( Error.PPError, cur );
				}

				cur++; c = ptr[cur]; // next '('
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }

				StringBuilder script = mWorkBuilder;
				script.Remove( 0, script.Length );

				int plevel = 0;
				while( c != 0 && (plevel != 0 || c != ')') ) {
					if( c == '(' ) plevel++;
					else if( c == ')' ) plevel--;
					script.Append( (char)c );
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				}
				if( c != 0 ) {
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				}

				parsePPExpression( script.ToString() );
				c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				mCurrent = cur;
				if( c == 0 ) return ENDED;
				return CONTINUE;
			}
			if( c == 'i' && ptr[cur+1] == 'f' ) {
				// if statement
				//mBlock.notifyUsingPreProcessror();
				cur+=2;
				c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				if( c == 0 ) {
					mCurrent = cur;
					throw new CompileException( Error.PPError, cur );
				}
				if( c != '(' ) {
					mCurrent = cur;
					throw new CompileException( Error.PPError, cur );
				}

				cur++; c = ptr[cur]; // next '('
				if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }

				StringBuilder script = mWorkBuilder;
				script.Remove( 0, script.Length );

				int plevel = 0;
				while( c != 0 && ( plevel != 0 || c != ')' ) ) {
					if( c == '(' ) plevel++;
					else if( c == ')' ) plevel--;
					script.Append( (char)c );
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				}
				if( c != 0 ) {
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
				}

				int ret = parsePPExpression( script.ToString() );
				if( ret == 0 ) {
					mCurrent = cur;
					return skipUntilEndif();
				}
				mIfLevel++;
				c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				mCurrent = cur;
				if( c == 0 ) return ENDED;
				return CONTINUE;
			}

			if( c == 'e' && ptr[cur+1] == 'n' && ptr[cur+2] == 'd'&& ptr[cur+3] == 'i' && ptr[cur+4] == 'f' ) {
				// endif statement
				cur+=5;
				mIfLevel--;
				if( mIfLevel < 0 ) {
					mCurrent = cur;
					throw new CompileException( Error.PPError, cur );
				}
				c = ptr[cur];
				while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
				mCurrent = cur;
				if( c == 0 ) return ENDED;
				return CONTINUE;
			}
			return NOT_COMMENT;
		}
		private int skipUntilEndif() {
			int exl = mIfLevel;
			mIfLevel++;
			char[] ptr = mText;
			int cur = mCurrent;
			char c = ptr[cur];
			while( true ) {
				if( c == '/' ) {
					// comment
					mCurrent = cur;
					int ret = parseComment();
					cur = mCurrent;
					switch( ret ) {
					case ENDED:
						mCurrent = cur;
						throw new CompileException( Error.PPError, cur );
					case CONTINUE:
						c = ptr[cur];
						break;
					case NOT_COMMENT:
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c == 0 ) {
							mCurrent = cur;
							throw new CompileException( Error.PPError, cur );
						}
						break;
					}
				} else if( c == '@' ) {
					cur++;
					c = ptr[cur];
					bool skipp = false;
					if( c == 'i' && ptr[cur+1] == 'f' ) {
						mIfLevel++;
						cur += 2;
						c = ptr[cur];
						skipp = true;
					} else if( c == 's' && ptr[cur+1] == 'e' && ptr[cur+2] == 't' ) {
						cur += 3;
						c = ptr[cur];
						skipp = true;
					} else if( c == 'e' && ptr[cur+1] == 'n' && ptr[cur+2] == 'd' && ptr[cur+3] == 'i' && ptr[cur+4] == 'f' ) {
						cur += 5;
						c = ptr[cur];
						mIfLevel--;
						if( mIfLevel == exl ) { // skip ended
							while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
							mCurrent = cur;
							if( c == 0 ) return ENDED;
							return CONTINUE;
						}
					} //else {}

					if( skipp ) {
						while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { cur++; c = ptr[cur]; }
						if( c == 0 ) {
							mCurrent = cur;
							throw new CompileException( Error.PPError, cur );
						}
						if( c != '(' ) {
							mCurrent = cur;
							throw new CompileException( Error.PPError, cur );
						}
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						int plevel = 0;
						while( c != 0 && ( plevel > 0 || c != ')' ) ) {
							if( c == '(' ) plevel++;
							else if( c == ')' ) plevel--;
							cur++; c = ptr[cur];
							if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						}
						if( c == 0 ) {
							mCurrent = cur;
							throw new CompileException( Error.PPError, cur );
						}
						cur++; c = ptr[cur];
						if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
						if( c == 0 ) {
							mCurrent = cur;
							throw new CompileException( Error.PPError, cur );
						}
					}
				} else {
					cur++; c = ptr[cur];
					if( c == CR && ptr[cur+1] == LF ) { cur++; c = ptr[cur]; }
					if( c == 0 ) {
						mCurrent = cur;
						throw new CompileException( Error.PPError, cur );
					}
				}
			}
		}
		private string escapeC( char c ) {
			switch( c ) {
				case (char)0x07: return ( "\\a" );
				case (char)0x08: return ( "\\b" );
				case (char)0x0c: return ( "\\f" );
				case (char)0x0a: return ( "\\n" );
				case (char)0x0d: return ( "\\r" );
				case (char)0x09: return ( "\\t" );
				case (char)0x0b: return ( "\\v" );
				case '\\': return( "\\\\" );
				case '\'': return( "\\\'" );
				case '\"': return( "\\\"" );
				default:
				if( c < 0x20 ) {
					StringBuilder ret = mWorkBuilder;
					const string HexAlphabet = "0123456789ABCDEF";
					ret.Remove( 0, ret.Length );
					ret.Append("\\x");
					ret.Append( HexAlphabet[(int)( c >> 4 )] );
					ret.Append( HexAlphabet[(int)( c & 0xf)] );
					return ret.ToString();
				} else {
					char[] v = new char[1];
					v[0] = c;
					return new string( v );
				}
			}
		}
		private int getToken() {
			char[] ptr = mText;
			char c = ptr[mCurrent];
			if( c == 0 ) return 0;
			if( mRegularExpression == true ) {
				mRegularExpression = false;
				mCurrent = mPrevPos;
				// 最初の'/'を読み飛ばし
				mCurrent++;
				if( mText[mCurrent] == CR && mText[mCurrent+1] == LF ) mCurrent++;
				String pattern = parseRegExp();
				mValue = putValue(pattern);
				return Token.T_REGEXP;
			}

			bool retry;
			do {
				retry = false;
				mPrevPos = mCurrent;
				c = ptr[mCurrent];
				switch(c) {
				case (char)0:
					return 0;
				case '>':
					mCurrent++; c = ptr[mCurrent];
					if( c == '>' ) {	// >>
						mCurrent++; c = ptr[mCurrent];
						if( c == '>' ) {	// >>>
							mCurrent++; c = ptr[mCurrent];
							if( c == '=' ) {	// >>>=
								mCurrent++;
								return Token.T_RBITSHIFTEQUAL;
							} else {
								return Token.T_RBITSHIFT;
							}
						} else if( c == '=' ) {	// >>=
							mCurrent++;
							return Token.T_RARITHSHIFTEQUAL;
						} else {	// >>
							return Token.T_RARITHSHIFT;
						}
					} else if( c == '=' ) {	// >=
						mCurrent++;
						return Token.T_GTOREQUAL;
					} else {
						return Token.T_GT;
					}

				case '<':
					mCurrent++; c = ptr[mCurrent];
					if( c == '<' ) {	// <<
						mCurrent++; c = ptr[mCurrent];
						if( c == '=' ) {	// <<=
							mCurrent++;
							return Token.T_LARITHSHIFTEQUAL;
						} else {	// <<
							return Token.T_LARITHSHIFT;
						}
					} else if( c == '-' ) {	// <-
						mCurrent++; c = ptr[mCurrent];
						if( c == '>' ) {	// <->
							mCurrent++;
							return Token.T_SWAP;
						} else {	// <
							mCurrent--;
							return Token.T_LT;
						}
					} else if( c == '=' ) {	// <=
						mCurrent++;
						return Token.T_LTOREQUAL;
					} else if( c == '%' ) {	// '<%'   octet literal
						MemoryStream buffer = parseOctet();
						mValue = putValue(buffer);
						return Token.T_CONSTVAL;
					} else {	// <
						return Token.T_LT;
					}

				case '=':
					mCurrent++; c = ptr[mCurrent];
					if( c == '=' ) { // ===
						mCurrent++; c = ptr[mCurrent];
						if( c == '=' ) {
							mCurrent++;
							return Token.T_DISCEQUAL;
						} else { // ==
							return Token.T_EQUALEQUAL;
						}
					} else if( c == '>' ) {	// =>
						mCurrent++;
						return Token.T_COMMA;
					} else {	// =
						return Token.T_EQUAL;
					}

				case '!':
					mCurrent++; c = ptr[mCurrent];
					if( c == '=' ) {
						mCurrent++; c = ptr[mCurrent];
						if( c == '=' ) { // !==
							mCurrent++;
							return Token.T_DISCNOTEQUAL;
						} else { // !=
							return Token.T_NOTEQUAL;
						}
					} else {	// !
						return Token.T_EXCRAMATION;
					}

				case '&':
					mCurrent++; c = ptr[mCurrent];
					if( c == '&' ) {
						mCurrent++; c = ptr[mCurrent];
						if( c == '=' ) { // &&=
							mCurrent++;
							return Token.T_LOGICALANDEQUAL;
						} else { // &&
							return Token.T_LOGICALAND;
						}
					} else if( c == '=' ) { // &=
						mCurrent++;
						return Token.T_AMPERSANDEQUAL;
					} else {	// &
						return Token.T_AMPERSAND;
					}

				case '|':
					mCurrent++; c = ptr[mCurrent];
					if( c == '|' ) {
						mCurrent++; c = ptr[mCurrent];
						if( c == '=' ) { // ||=
							mCurrent++;
							return Token.T_LOGICALOREQUAL;
						} else { // ||
							return Token.T_LOGICALOR;
						}
					} else if( c == '=' ) { // |=
						mCurrent++;
						return Token.T_VERTLINEEQUAL;
					} else { // |
						return Token.T_VERTLINE;
					}

				case '.':
					mCurrent++; c = ptr[mCurrent];
					if( c >= '0' && c <= '9' ) { // number
						mCurrent--;
						//mCurrent--;
						Number o = parseNumber();
						if( o != null ) {
							if( o.type == Number.TYPE_INT )
								mValue = putValue((Int64)o.lvalue);
							else if( o.type == Number.TYPE_DOUBLE )
								mValue = putValue((Double)o.dvalue);
							else
								mValue = putValue(null);
						} else {
							mValue = putValue(null);
						}
						return Token.T_CONSTVAL;
					} else if( c == '.' ) {
						mCurrent++; c = ptr[mCurrent];
						if( c == '.' ) { // ...
							mCurrent++;
							return Token.T_OMIT;
						} else { // .
							mCurrent--;
							//mCurrent--;
							return Token.T_DOT;
						}
					} else { // .
						return Token.T_DOT;
					}

				case '+':
					mCurrent++; c = ptr[mCurrent];
					if( c == '+' ) { // ++
						mCurrent++;
						return Token.T_INCREMENT;
					} else if( c == '=' ) { // +=
						mCurrent++;
						return Token.T_PLUSEQUAL;
					} else { // +
						return Token.T_PLUS;
					}

				case '-':
					mCurrent++; c = ptr[mCurrent];
					if( c == '-' ) { // --
						mCurrent++;
						return Token.T_DECREMENT;
					} else if( c == '=' ) {
						mCurrent++;
						return Token.T_MINUSEQUAL; // -=
					} else { // -
						return Token.T_MINUS;
					}

				case '*':
					mCurrent++; c = ptr[mCurrent];
					if( c == '=' ) { // *=
						mCurrent++;
						return Token.T_ASTERISKEQUAL;
					} else { // *
						return Token.T_ASTERISK;
					}

				case '/':
					mCurrent++; c = ptr[mCurrent];
					if( c == '/' || c == '*' ) {
						mCurrent--;
						int comment = parseComment();
						if( comment == CONTINUE ) {
							return Token.T_COMMENT;
						} else if( comment == ENDED ) {
							return Token.T_COMMENT;
							// return 0;
						}
					}
					if( c == '=' ) {	// /=
						mCurrent++;
						return Token.T_SLASHEQUAL;
					} else {	// /
						return Token.T_SLASH;
					}

				case '\\':
					mCurrent++; c = ptr[mCurrent];
					if( c == '=' ) {	// \=
						mCurrent++;
						return Token.T_BACKSLASHEQUAL;
					} else {	// \
						return Token.T_BACKSLASH;
					}

				case '%':
					mCurrent++; c = ptr[mCurrent];
					if( c == '=' ) { // %=
						mCurrent++;
						return Token.T_PERCENTEQUAL;
					} else { // %
						return Token.T_PERCENT;
					}

				case '^':
					mCurrent++; c = ptr[mCurrent];
					if( c == '=' ) { // ^=
						mCurrent++;
						return Token.T_CHEVRONEQUAL;
					} else { // ^
						return Token.T_CHEVRON;
					}

				case '[':
					mNestLevel++;
					mCurrent++;
					return Token.T_LBRACKET;

				case ']':
					mNestLevel--;
					mCurrent++;
					return Token.T_RBRACKET;

				case '(':
					mNestLevel++;
					mCurrent++;
					return Token.T_LPARENTHESIS;

				case ')':
					mNestLevel--;
					mCurrent++;
					return Token.T_RPARENTHESIS;

				case '~':
					mCurrent++;
					return Token.T_TILDE;

				case '?':
					mCurrent++;
					return Token.T_QUESTION;

				case ':':
					mCurrent++;
					return Token.T_COLON;

				case ',':
					mCurrent++;
					return Token.T_COMMA;

				case ';':
					mCurrent++;
					return Token.T_SEMICOLON;

				case '{':
					mNestLevel++;
					mCurrent++;
					return Token.T_LBRACE;

				case '}':
					mNestLevel--;
					mCurrent++;
					return Token.T_RBRACE;

				case '#':
					mCurrent++;
					return Token.T_SHARP;

				case '$':
					mCurrent++;
					return Token.T_DOLLAR;

				case '\'':
				case '\"': {
					// literal string
					//String str = parseString(c);
					//next();
					mCurrent++;
					if( mText[mCurrent] == CR && mText[mCurrent+1] == LF ) mCurrent++;
					String str = readString(c,false);
					mValue = putValue(str);
					return Token.T_CONSTVAL;
				}

				case '@': {
					// embeddable expression in string (such as @"this can be embeddable like &variable;")
					int org = mCurrent;
					mCurrent++; c = ptr[mCurrent];
					if( c == CR && ptr[mCurrent+1] == LF ) { mCurrent++; c = ptr[mCurrent]; }
					while( c != 0 && c <= SPACE && (c == CR || c == LF || c == TAB || c == SPACE) ) { mCurrent++; c = ptr[mCurrent]; }
					if( c == 0 ) return 0;
					if( c == '\'' || c == '\"' ) {
						EmbeddableExpressionData data = new EmbeddableExpressionData();
						data.mState = EmbeddableExpressionData.START;
						data.mWaitingNestLevel = mNestLevel;
						data.mDelimiter = c;
						data.mNeedPlus = false;
						mCurrent++; c = ptr[mCurrent];
						if( c == CR && ptr[mCurrent+1] == LF ) { mCurrent++; c = ptr[mCurrent]; }
						if( c == 0 ) return 0;
						mEmbeddableExpressionDataStack.Add( data );
						return -1;
					} else {
						mCurrent = org;
					}
					// possible pre-procesor statements
					switch( processPPStatement() ) {
					case CONTINUE:
						retry = true;
						break;
					case ENDED:
						return 0;
					case NOT_COMMENT:
						mCurrent = org;
						break;
					}
					break;
				}
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9': {
					Number o = parseNumber();
					if( o != null ) {
						if( o.type == Number.TYPE_INT )
							mValue = putValue((Int64)o.lvalue);
						else if( o.type == Number.TYPE_DOUBLE )
							mValue = putValue((Double)o.dvalue);
						else
							throw new CompileException( Error.NumberError, mCurrent );
					} else {
						throw new CompileException( Error.NumberError, mCurrent );
					}
					return Token.T_CONSTVAL;
				}
				}	// switch(c)
			} while( retry );

			if( (((c & 0xFF00) != 0 ) || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) == false && c != '_' ) {
				string str = Error.InvalidChar.Replace( "%1", escapeC((char)c) );
				throw new CompileException( str, mCurrent );
			}
			int oldC = c;
			int offset = mCurrent;
			int nch = 0;
			while( (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')|| c == '_' || ( c >= '0' && c <= '9' ) || ((c & 0xFF00) != 0 ) ) {
				mCurrent++; c = ptr[mCurrent];
				nch++;
			}
			if( nch == 0 ) {
				string str = Error.InvalidChar.Replace( "%1", escapeC((char)oldC) );
				throw new CompileException( str, mCurrent );
			}

			string strval = new string( ptr, offset, nch );
			int retnum;
			if( mBareWord ) {
				retnum = -1;
				mBareWord = false;
			} else {
				retnum = ReservedWordToken.getToken( strval );
			}

			if( retnum == -1 ) { // not a reserved word
				mValue = putValue( strval );
				return Token.T_SYMBOL;
			}
			switch( retnum ) {
			case Token.T_FALSE:
				mValue = putValue( 0 );
				return Token.T_CONSTVAL;
			case Token.T_NULL:
				mValue = putValue( new VariantClosure(null) );
				return Token.T_CONSTVAL;
			case Token.T_TRUE:
				mValue = putValue( 1 );
				return Token.T_CONSTVAL;
			case Token.T_NAN:
				mValue = putValue( Double.NaN );
				return Token.T_CONSTVAL;
			case Token.T_INFINITY:
				mValue = putValue( Double.PositiveInfinity );
				return Token.T_CONSTVAL;
			}
			return retnum;
		}
		public int getValue() { return mValue; }
		
		public int getNext() {
			if( mIsFirst ) {
				mIsFirst = false;
				if( mIsExprMode && mResultNeeded ) {
					mValue = 0;
					return Token.T_RETURN;
				}
			}
			int n = 0;
			mValue = 0;
			do {
				if( mRetValDeque.Count > 0 ) {
					long pair = mRetValDeque.Dequeue();
					mValue = (int) (pair >> 32);
					mPrevToken = (int) (pair & 0xffffffffL);
					return mPrevToken;
				}
				try {
					if( mEmbeddableExpressionDataStack.Count == 0 ) {
						char c  = mText[mCurrent];
						while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { mCurrent++; c = mText[mCurrent]; }
						n = getToken();
						/*
						if( CompileState.mEnableDicFuncQuickHack ) { // dicfunc quick-hack
							if( mDicFunc ) {
								if( n == Token.T_PERCENT ) {
									// push "function { return %"
									mRetValDeque.push_back( Token.T_FUNCTION ); // value index は 0 なので無視
									mRetValDeque.push_back( Token.T_LBRACE );
									mRetValDeque.push_back( Token.T_RETURN );
									mRetValDeque.push_back( Token.T_PERCENT );
									n = -1;
								} else if ( n == Token.T_LBRACKET && mPrevToken != Token.T_PERCENT ) {
									// push "function { return ["
									mRetValDeque.push_back( Token.T_FUNCTION ); // value index は 0 なので無視
									mRetValDeque.push_back( Token.T_LBRACE );
									mRetValDeque.push_back( Token.T_RETURN );
									mRetValDeque.push_back( Token.T_LBRACKET );
									n = -1;
								} else if( n == Token.T_RBRACKET ) {
									// push "] ; } ( )"
									mRetValDeque.push_back( Token.T_RBRACKET ); // value index は 0 なので無視
									mRetValDeque.push_back( Token.T_SEMICOLON );
									mRetValDeque.push_back( Token.T_RBRACE );
									mRetValDeque.push_back( Token.T_LPARENTHESIS );
									mRetValDeque.push_back( Token.T_RPARENTHESIS );
									n = -1;
								}
							}
						}
						*/
					} else {
						/*
						// embeddable expression mode
						EmbeddableExpressionData data = mEmbeddableExpressionDataStack.get( mEmbeddableExpressionDataStack.size() -1 );
						switch( data.mState ) {
						case EmbeddableExpressionData.START:
							mRetValDeque.push_back( Token.T_LPARENTHESIS ); // value index は 0 なので無視
							n = -1;
							data.mState = EmbeddableExpressionData.NEXT_IS_STRING_LITERAL;
							break;

						case EmbeddableExpressionData.NEXT_IS_STRING_LITERAL: {
							String str = readString( data.mDelimiter, true );
							int res = mStringStatus;
							if( mStringStatus == DELIMITER ) {
								// embeddable expression mode ended
								if( str.length() > 0 ) {
									if( data.mNeedPlus ) {
										mRetValDeque.push_back( Token.T_PLUS ); // value index は 0 なので無視
									}
								}
								if( str.length() > 0 || data.mNeedPlus == false ) {
									int v = putValue(str);
									mRetValDeque.push_back( Token.T_CONSTVAL | (v<<32) );
								}
								mRetValDeque.push_back( Token.T_RPARENTHESIS ); // value index は 0 なので無視
								mEmbeddableExpressionDataStack.remove( mEmbeddableExpressionDataStack.size() - 1 );
								n = -1;
								break;
							} else {
								// c is next to ampersand mark or '${'
								if( str.length() > 0 ) {
									if( data.mNeedPlus ) {
										mRetValDeque.push_back( Token.T_PLUS ); // value index は 0 なので無視
									}
									int v = putValue(str);
									mRetValDeque.push_back( Token.T_CONSTVAL | (v<<32) );
									data.mNeedPlus = true;
								}
								if( data.mNeedPlus == true ) {
									mRetValDeque.push_back( Token.T_PLUS ); // value index は 0 なので無視
								}
								mRetValDeque.push_back( Token.T_STRING ); // value index は 0 なので無視
								mRetValDeque.push_back( Token.T_LPARENTHESIS );
								data.mState = EmbeddableExpressionData.NEXT_IS_EXPRESSION;
								if( res == AMPERSAND ) {
									data.mWaitingToken = Token.T_SEMICOLON;
								} else if( res == DOLLAR ) {
									data.mWaitingToken = Token.T_RBRACE;
									mNestLevel++;
								}
								n = -1;
								break;
							}
						}
						case EmbeddableExpressionData.NEXT_IS_EXPRESSION:
							//skipSpace();
							char c  = mText[mCurrent];
							while( c != 0 && c <= SPACE && (c == CR || c == LF|| c == TAB || c == SPACE) ) { mCurrent++; c = mText[mCurrent]; }
							n = getToken();
							if( n == data.mWaitingToken && mNestLevel == data.mWaitingNestLevel ) {
								// end of embeddable expression mode
								mRetValDeque.push_back( Token.T_RPARENTHESIS ); // value index は 0 なので無視
								data.mNeedPlus = true;
								data.mState = EmbeddableExpressionData.NEXT_IS_STRING_LITERAL;
								n = -1;
							}
							break;
						}
						*/
					}
					if( n == 0 ) {
						if( mIfLevel != 0 ) {
							throw new CompileException( Error.PPError, mCurrent );
						}
					}
				} catch( CompileException e ) {
					// mBlock.error( e.getMessage() );
					return 0;
				}
			} while ( n < 0 );
			mPrevToken = n;
			return n;
		}
		public int getCurrentPosition() { return mCurrent; }
	}
}
