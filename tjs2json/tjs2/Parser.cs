using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace TJS2 {
	class Parser {
		class TokenInfo {
			public int Token;
			public object Value;
			public TokenInfo( int token, object value ) {
				Token = token;
				Value = value;
			}
		}
		public class ScriptNode {
			public int Type;			// class, function, property, var
			public string Comment;
			public string Name;
			public List<string> Args;	// for function
			public List<ScriptNode> Child;
			public ScriptNode( int type, string name, string comment ) {
				Child = null;
				Args = null;
				if( type == Token.T_FUNCTION ) {
					Args = new List<string>();
				} else if( type == Token.T_CLASS || type == 0 ) {
					Child = new List<ScriptNode>();
				}
				Type = type;
				Name = name;
				Comment = comment;
			}
		}
		List<TokenInfo> tokens_;
		int error_line_;

		private bool parse( string script ) {
			error_line_ = -1;
			tokens_ = new List<TokenInfo>();
			LexicalAnalyzer lex = new LexicalAnalyzer( script, false, false );
			ScriptLineData lines = new ScriptLineData( script, 0 );
			int token = 0;
			try {
				do {
					token = lex.getNext();
					if( token == Token.T_SYMBOL ) {
						string name = lex.getString( lex.getValue() );
						tokens_.Add( new TokenInfo( token, name ) );
					} else if( token == Token.T_COMMENT ) {
						string comment = lex.getComment();
						tokens_.Add( new TokenInfo( token, comment ) );
					} else {
						// シンボル名とコメント本体のみ取得して、他は値無視
						tokens_.Add( new TokenInfo( token, null ) );
					}
				} while( token != 0 );
			} catch( CompileException ) {
				error_line_ = lines.getSrcPosToLine( lex.getCurrentPosition() );
			}
			return error_line_ == -1;
		}
		private ScriptNode createNodes( string script ) {
			if( parse( script ) == false) {
				System.Console.WriteLine( "Parse Error. Line: " + error_line_ );
				return null;
			}
			string comment = null;
			string name = null;
			int type = 0;
			bool prevComment = false;
			int nest = 0;
			ScriptNode topNode = new ScriptNode( 0, null, null );
			ScriptNode currentNode = topNode;
			Stack<ScriptNode> node = new Stack<ScriptNode>();
			node.Push( topNode );
			Stack<int> classNest = new Stack<int>();
			classNest.Push( -2 );
			int i = 0;
			string headerComment = null;
			if( tokens_.Count > 0 && tokens_[0].Token == Token.T_COMMENT ) {
				// header comment
				i++;
				headerComment = (string)tokens_[0].Value;
				if( tokens_.Count > 1 ) {
					int token = tokens_[1].Token;
					if( token == Token.T_FUNCTION ||
						token == Token.T_PROPERTY ||
						token == Token.T_CLASS ||
						token == Token.T_VAR ||
						token == Token.T_CONST ) {
						// ヘッダーコメントではなく、次のメンバのコメントとなる
						headerComment = null;
						i--;
					}
				}
			}
			if( headerComment != null ) {
				topNode.Comment = headerComment;
			}
			for( ; i < tokens_.Count; i++ ) {
				TokenInfo token = tokens_[i];
				switch( token.Token ) {
					case Token.T_COMMENT:
						comment = (string)token.Value;
						prevComment = true;
						break;
					case Token.T_SYMBOL:
						name = (string)token.Value;
						if( type != 0 ) {
							if( prevComment != true ) {
								comment = null;
							}
							ScriptNode n = new ScriptNode( type, name, comment );
							currentNode.Child.Add( n );
							if( type == Token.T_CLASS ) {
								node.Push( n );
								currentNode = n;
							}
							if( type == Token.T_FUNCTION ) {
								// 関数なので引数をチェックする
								i++;
								if( i < tokens_.Count ) {
									token = tokens_[i];
									if( token.Token != Token.T_LPARENTHESIS ) {
										i--;
										continue;
									}
								}
								// Token.T_LPARENTHESIS (
								// Token.T_RPARENTHESIS )
								// Token.T_COMMA ,
								// Token.T_COLON :
								// Token.T_SYMBOL
								// Token.T_LBRACE {
								// Token.T_INT int
								// Token.T_REAL real
								// Token.T_STRING string
								// Token.T_OCTET octet
								// Token.T_OMIT ...
								// * や初期値などは調べてない
								bool nextSymbol = true;
								for( ; i < tokens_.Count; i++ ) {
									token = tokens_[i];
									if( token.Token == Token.T_RPARENTHESIS ) {	// )
										break;
									}
									if( nextSymbol && token.Token == Token.T_SYMBOL ) {
										n.Args.Add( (string)token.Value );
										nextSymbol = false;
									} else if( token.Token == Token.T_OMIT ) {
										n.Args.Add( "..." );
										nextSymbol = false;
									} else if( token.Token == Token.T_COMMA ) {
										nextSymbol = true;
									}
								}

							}
						}
						break;
					case Token.T_FUNCTION:
						type = Token.T_FUNCTION;
						break;
					case Token.T_PROPERTY:
						type = Token.T_PROPERTY;
						break;
					case Token.T_CLASS:
						type = Token.T_CLASS;
						classNest.Push( nest );
						break;
					case Token.T_VAR:
						type = Token.T_VAR;
						break;
					case Token.T_CONST:
						type = Token.T_CONST;
						break;
					case Token.T_LBRACE:
						nest++;
						prevComment = false; type = 0;
						break;
					case Token.T_RBRACE:
						nest--;
						if( nest == classNest.Peek() ) {
							// 現在のクラスノードが終了した
							node.Pop();
							classNest.Pop();
							if( node.Count > 0 ) {
								currentNode = node.Peek();
							}
						}
						prevComment = false; type = 0;
						break;
					default:
						prevComment = false; type = 0;
						break;
				}
			}
			return topNode;
		}
		class Param {
			public string Name;
			public string Desc;
			public Param( string name, string desc ) {
				Name = name;
				Desc = desc;
			}
		}
		private void traverseNode( ScriptNode node, ref StreamWriter writer, bool isLast ) {
			writer.WriteLine( "{" );
			writer.Write( "\"type\":" );
			switch( node.Type ) {
				case 0:
					writer.WriteLine( "\"root\"," );
					break;
				case Token.T_FUNCTION:
					writer.WriteLine( "\"function\"," );
					break;
				case Token.T_PROPERTY:
					writer.WriteLine( "\"property\"," );
					break;
				case Token.T_CLASS:
					writer.WriteLine( "\"class\"," );
					break;
				case Token.T_VAR:
					writer.WriteLine( "\"var\"," );
					break;
				case Token.T_CONST:
					writer.WriteLine( "\"const\"," );
					break;
				default:
					writer.WriteLine( "\"unknown\"," );
					break;
			}
			if( node.Type == Token.T_FUNCTION && node .Args != null && node.Args.Count > 0 ) {
				writer.WriteLine( "\"arguments\":[" );
				for( int i = 0; i < node.Args.Count; i++ ) {
					string arg = node.Args[i];
					if( ( i + 1 ) != node.Args.Count ) {
						writer.WriteLine( "\"" + arg + "\"," );
					} else {
						writer.WriteLine( "\"" + arg + "\"" );
					}
				}
				if( node.Comment != null || node.Child != null ) {
					writer.WriteLine( "]," );
				} else {
					writer.WriteLine( "]" );
				}
			} 
			if( node.Comment != null || node.Child != null ) {
				writer.WriteLine( "\"name\":\"" + node.Name + "\"," );
			} else {
				writer.WriteLine( "\"name\":\"" + node.Name + "\"" );
			}
			if( node.Comment != null ) {
				writer.WriteLine( "\"comment\":{" );
				string comment = escapeJsonString( node.Comment );
				List<CommentNode> cnl = parseComment( node.Comment );
				if( cnl.Count > 0 ) {
					writer.WriteLine( "\"raw\":\"" + comment + "\"," );
				} else {
					writer.WriteLine( "\"raw\":\"" + comment + "\"" );
				}
				List<Param> parameters = new List<Param>();
				for( int i = 0; i < cnl.Count; i++ ) {
					CommentNode cn = cnl[i];
					string body = Regex.Replace( cn.Body, "[ \t]*\\\\n$", "" );	// 末尾の空白と改行は削除する
					body = escapeJsonString( body );
					switch( cn.Type ) {
						case CommentType.SUMMARY:
							writer.Write( "\"summary\":" );
							break;
						case CommentType.PARAM:
							//writer.Write( "\"param\":{\"name\":\"" + cn.Name + "\",\"desc\":\"" + body + "\"}" );
							parameters.Add( new Param( cn.Name, body ) );
							break;
						case CommentType.RETURN:
							writer.Write( "\"return\":" );
							break;
						case CommentType.THROW:
							writer.Write( "\"throw\":" );
							break;
						case CommentType.AUTHOR:
							writer.Write( "\"author\":" );
							break;
						case CommentType.VERSION:
							writer.Write( "\"version\":" );
							break;
						case CommentType.SEE:
							writer.Write( "\"see\":" );
							break;
						case CommentType.DESCRIPTION:
							writer.Write( "\"description\":" );
							break;
						default:
							writer.Write( "\"unknown\":" );
							break;
					}
					if( cn.Type != CommentType.PARAM ) {
						writer.Write( "\"" + body + "\"" );
					}
					if( ( i + 1 ) != cnl.Count || parameters.Count > 0 ) {
						writer.Write( "," );
					}
					writer.WriteLine( "" );
				}
				if( parameters.Count > 0 ) {
					writer.WriteLine( "\"param\":[" );
					for( int i = 0; i < parameters.Count; i++ ) {
						Param p = parameters[i];
						writer.Write( "{\"name\":\"" + p.Name + "\",\"desc\":\"" + p.Desc + "\"}" );
						if( ( i + 1 ) != parameters.Count ) {
							writer.WriteLine( "," );
						} else {
							writer.WriteLine( "" );
						}
					}
					writer.WriteLine( "]" );
				}

				if( node.Child != null ) {
					writer.WriteLine( "}," );
				} else {
					writer.WriteLine( "}" );
				}
			}
			if( node.Child != null ) {
				writer.Write( "\"members\":[" );
				int count = node.Child.Count;
				int index = 0;
				foreach( ScriptNode n in node.Child ) {
					traverseNode( n, ref writer, ( index + 1 ) == count );
					index++;
				}
				writer.Write( "]" );
			}
			if( isLast ) {
				writer.WriteLine( "}" );
			} else {
				writer.WriteLine( "}," );
			}
		}
		public void toJson( string filename, string output, string encoding ) {
			string script = "";
			try {
				using( StreamReader sr = new StreamReader(
					filename, Encoding.GetEncoding( encoding ) ) ) {
					script = sr.ReadToEnd();
				}
			} catch( Exception e ) {
				Console.WriteLine( e.Message );
				return;
			}
			ScriptNode node = createNodes( script );
			if( node != null ) {
				SerializeNode parent = null;
				SerializeNode sn = SerializeNode.translateNode( ref parent, node );
				if( sn != null ) {
					Stream stream = new FileStream( output, FileMode.Create );
					DataContractJsonSerializer serialize = new DataContractJsonSerializer( typeof( SerializeNode ) );
					serialize.WriteObject( stream, sn );
					stream.Close();
				}
				/*
				StreamWriter writer = new StreamWriter( output, false, Encoding.UTF8 );
				traverseNode( node, ref writer, true );
				writer.Close();
				*/
			}
		}
		private string escapeJsonString( string input ) {
			input = input.Replace( "\\", "\\\\" );
			input = input.Replace( "\"", "\\\"" );
			input = input.Replace( "\r\n", "\\n" );
			input = input.Replace( "\t", "\\t" );
			input = input.Replace( "/", "\\/" );
			return input;
		}
		public enum CommentType {
			SUMMARY,
			PARAM,
			RETURN,
			THROW,
			AUTHOR,
			VERSION,
			SEE,
			DESCRIPTION,
		}
		public class CommentNode {
			public CommentType Type;
			public string Name;
			public string Body;
			public CommentNode( CommentType type, string body ) {
				Type = type;
				Body = body;
				Name = null;
			}
			public CommentNode( CommentType type, string name, string body ) {
				Type = type;
				Name = name;
				Body = body;
			}
		}
		static public List<CommentNode> parseComment( string comment ) {
			List<CommentNode> result = new List<CommentNode>();
			System.IO.StringReader rs = new System.IO.StringReader( comment );
			CommentNode current = new CommentNode( CommentType.SUMMARY, "" );
			result.Add( current );
			while( rs.Peek() >= 0 ) {
				string line = rs.ReadLine();
				line = Regex.Replace( line, "^[ \t]*\\*[ \t]*", "" ); // 行頭の * を取り除く
				line = Regex.Replace( line, "^[ \t]*", "" ); // 行頭のスペースを取り除く
				line = Regex.Replace( line, "-{4,}", "" ); // -の4回以上の繰り返しを取り除く
				if( Regex.Match( line, "^@param" ).Success ) {
					Match match;
					if( ( match = Regex.Match( line, "^@param[ \t]*([^ \t]+)[ \t]+(.+)" ) ) != Match.Empty ) {
						string name  = match.Groups[1].Value;
						string body = match.Groups[2].Value;
						current = new CommentNode( CommentType.PARAM, name, body );
						result.Add( current );
					} else {
						string body = Regex.Replace( line, "^@param[ \t]*", "" ); // 先頭部分を削除
						current = new CommentNode( CommentType.PARAM, body );
						result.Add( current );
					}
				} else if( Regex.Match( line, "^@return" ).Success ) {
					string body = Regex.Replace( line, "^@return[ \t]*", "" );
					current = new CommentNode( CommentType.RETURN, body );
					result.Add( current );
				} else if( Regex.Match( line, "^@throw" ).Success ) {
					string body = Regex.Replace( line, "^@throw[ \t]*", "" );
					current = new CommentNode( CommentType.THROW, body );
					result.Add( current );
				} else if( Regex.Match( line, "^@author" ).Success ) {
					string body = Regex.Replace( line, "^@author[ \t]*", "" );
					current = new CommentNode( CommentType.AUTHOR, body );
					result.Add( current );
				} else if( Regex.Match( line, "^@version" ).Success ) {
					string body = Regex.Replace( line, "^@version[ \t]*", "" );
					current = new CommentNode( CommentType.VERSION, body );
					result.Add( current );
				} else if( Regex.Match( line, "^@see" ).Success ) {
					string body = Regex.Replace( line, "^@see[ \t]*", "" );
					current = new CommentNode( CommentType.SEE, body );
					result.Add( current );
				} else if( Regex.Match( line, "^@description" ).Success ) {
					string body = Regex.Replace( line, "^@description[ \t]*", "" );
					current = new CommentNode( CommentType.DESCRIPTION, body );
					result.Add( current );
				} else {
					// previous type
					if( current.Body.Length > 0 ) {
						current.Body += "\\n" + line;
					} else {
						current.Body = line;
					}
				}
			}
			return result;
		}
	}
}

