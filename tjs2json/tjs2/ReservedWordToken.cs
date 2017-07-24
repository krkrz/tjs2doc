using System;
using System.Collections.Generic;
using System.Text;

namespace TJS2 {
	class ReservedWordToken {
		public static int getToken( string str ) {
			int len = str.Length;
			char ch;
			switch( len ) {
			case 2:
				ch = str[1];
				if( ch == 'o' && str[0] == 'd' ) return Token.T_DO; // "do";
				else if( ch == 'n' && str[0] == 'i' ) return Token.T_IN; // "in";
				else if( ch == 'f' && str[0] == 'i' ) return Token.T_IF; // "if";
				break;
			case 3:
				switch( str[0] ) {
				case 'f':
					if( str[2] == 'r' && str[1] == 'o' ) return Token.T_FOR; // for
					break;
				case 'i':
					if( str[2] == 't' && str[1] == 'n' ) return Token.T_INT; // int
					break;
				case 'n':
					if( str[2] == 'w' && str[1] == 'e' ) return Token.T_NEW; // new
					break;
				case 't':
					if( str[2] == 'y' && str[1] == 'r' ) return Token.T_TRY; // try
					break;
				case 'v':
					if( str[2] == 'r' && str[1] == 'a' ) return Token.T_VAR; // var
					break;
				case 'N':
					if( str[2] == 'N' && str[1] == 'a' ) return Token.T_NAN; // NaN
					break;
				}
				break;
			case 4:
				switch( str[0] ) {
				case 'c':
					if( "case".Equals(str) ) return Token.T_CASE;
					break;
				case 'e':
					ch = str[1];
					if( ch == 'n' && str[3] == 'm' && str[2] == 'u' ) return Token.T_ENUM; // enum
					else if( ch == 'l' && str[3] == 'e' && str[2] == 's' ) return Token.T_ELSE; // else
					break;
				case 'g':
					if( "goto".Equals(str) ) return Token.T_GOTO;
					break;
				case 'n':
					if( "null".Equals(str) ) return Token.T_NULL;
					break;
				case 'r':
					if( "real".Equals(str) ) return Token.T_REAL;
					break;
				case 't':
					ch = str[1];
					if( ch == 'h' && str[3] == 's' && str[2] == 'i' ) return Token.T_THIS; // this
					else if( ch == 'r' && str[3] == 'e' && str[2] == 'u') return Token.T_TRUE; // true
					break;
				case 'v':
					if( "void".Equals(str) ) return Token.T_VOID;
					break;
				case 'w':
					if( "with".Equals(str) ) return Token.T_WITH;
					break;
				}
				break;
			case 5:
				switch( str[0] ) {
				case 'b':
					if( "break".Equals(str) ) return Token.T_BREAK;
					break;
				case 'c':
					ch = str[1];
					if( ch == 'o' && "const".Equals(str) ) return Token.T_CONST;
					else if( ch == 'a' && "catch".Equals(str) ) return Token.T_CATCH;
					else if( ch == 'l' && "class".Equals(str) ) return Token.T_CLASS;
					break;
				case 'f':
					if( "false".Equals(str) ) return Token.T_FALSE;
					break;
				case 'o':
					if( "octet".Equals(str) ) return Token.T_OCTET;
					break;
				case 's':
					if( "super".Equals(str) ) return Token.T_SUPER;
					break;
				case 't':
					if( "throw".Equals(str) ) return Token.T_THROW;
					break;
				case 'w':
					if( "while".Equals(str) ) return Token.T_WHILE;
					break;
				case 'e':
					if( "event".Equals( str ) ) return Token.T_EVENT;
					break;
				}
				break;
			case 6:
				switch( str[0] ) {
				case 'd':
					if( "delete".Equals(str) ) return Token.T_DELETE;
					break;
				case 'e':
					if( "export".Equals(str) ) return Token.T_EXPORT;
					break;
				case 'g':
					ch = str[1];
					if( ch == 'l' && "global".Equals(str) ) return Token.T_GLOBAL;
					else if( ch == 'e' && "getter".Equals(str) ) return Token.T_GETTER;
					break;
				case 'i':
					if( "import".Equals(str) ) return Token.T_IMPORT;
					break;
				case 'p':
					if( "public".Equals(str) ) return Token.T_PUBLIC;
					break;
				case 'r':
					if( "return".Equals(str) ) return Token.T_RETURN;
					break;
				case 's':
					switch( str[2] ) {
					case 't':
						if( "setter".Equals(str) ) return Token.T_SETTER;
						break;
					case 'a':
						if( "static".Equals(str) ) return Token.T_STATIC;
						break;
					case 'r':
						if( "string".Equals(str) ) return Token.T_STRING;
						break;
					case 'i':
						if( "switch".Equals(str) ) return Token.T_SWITCH;
						break;
					}
					break;
				case 't':
					if( "typeof".Equals(str) ) return Token.T_TYPEOF;
					break;
				}
				break;
			case 7:
				switch( str[0] ) {
				case 'd':
					if( "default".Equals(str) ) return Token.T_DEFAULT;
					break;
				case 'e':
					if( "extends".Equals(str) ) return Token.T_EXTENDS;
					break;
				case 'f':
					if( "finally".Equals(str) ) return Token.T_FINALLY;
					break;
				case 'i':
					if( "isvalid".Equals(str) ) return Token.T_ISVALID;
					break;
				case 'p':
					if( "private".Equals(str) ) return Token.T_PRIVATE;
					break;
				}
				break;
			case 8:
				switch( str[0] ) {
				case 'c':
					if( "continue".Equals(str) ) return Token.T_CONTINUE;
					break;
				case 'd':
					if( "debugger".Equals(str) ) return Token.T_DEBUGGER;
					break;
				case 'f':
					if( "function".Equals(str) ) return Token.T_FUNCTION;
					break;
				case 'p':
					if( "property".Equals(str) ) return Token.T_PROPERTY;
					break;
				case 'I':
					if( "Infinity".Equals(str) ) return Token.T_INFINITY;
					break;
				}
				break;
			case 9:
				if( "protected".Equals(str) ) return Token.T_PROTECTED;
				break;
			case 10:
				ch = str[9];
				if( ch == 'e' && "invalidate".Equals(str) ) return Token.T_INVALIDATE;
				else if( ch == 'f' && "instanceof".Equals(str) ) return Token.T_INSTANCEOF;
				break;
			case 11:
				if( "incontextof".Equals(str) ) return Token.T_INCONTEXTOF;
				break;
			case 12:
				if( "synchronized".Equals(str) ) return Token.T_SYNCHRONIZED;
				break;
			}
			return -1;
		}
	}
}
