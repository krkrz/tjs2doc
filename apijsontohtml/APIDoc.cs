using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;

namespace apijsontohtml {
	[DataContract]
	class FunctionParam {
		[DataMember( Name = "name" )]
		public string Name { get; set; }

		[DataMember( Name = "desc" )]
		public string Description { get; set; }
	}
	[DataContract]
	class FunctionArg {
		[DataMember( Name = "name" )]
		public string Name { get; set; }

		[DataMember( Name = "type" )]
		public string Type { get; set; }

		[DataMember( Name = "default" )]
		public string Default { get; set; }
	}
	[DataContract]
	class ScriptComment {
		[DataMember( Name = "raw" )]
		public string Raw { get; set; }

		[DataMember( Name = "summary" )]
		public string Summary { get; set; }

		[DataMember( Name = "return" )]
		public string Return { get; set; }

		[DataMember( Name = "throw" )]
		public string Throw { get; set; }

		[DataMember( Name = "author" )]
		public string Author { get; set; }

		[DataMember( Name = "version" )]
		public string Version { get; set; }

		[DataMember( Name = "see" )]
		public string See { get; set; }

		[DataMember( Name = "description" )]
		public string Description { get; set; }

		[DataMember( Name = "unknown" )]
		public string Unknown { get; set; }

		[DataMember( Name = "param" )]
		public List<FunctionParam> Params { get; set; }

		public ScriptComment() {
			Raw = "";
			Summary = "";
			Return = "";
			Throw = "";
			Author = "";
			Version = "";
			See = "";
			Description = "";
			Unknown = "";
			Params = new List<FunctionParam>();
		}
	}
	[DataContract]
	class ScriptNode {
		public static MarkdownSharp.Markdown Markdown;
		public enum NodeType {
			Unknown,
			Root,
			Function,
			Property,
			Class,
			Var,
			Const,
			Event,
		}
		public const string TYPE_ROOT = "root";
		public const string TYPE_FUNCTION = "function";
		public const string TYPE_PROPERTY = "property";
		public const string TYPE_CLASS = "class";
		public const string TYPE_VAR = "var";
		public const string TYPE_CONST = "const";
		public const string TYPE_UNKNOWN = "unknown";
		public const string TYPE_EVENT = "event";

		public NodeType Type {
			get {
				string name = TypeName;
				if( TYPE_ROOT.Equals( name ) ) return NodeType.Root;
				else if( TYPE_FUNCTION.Equals( name ) ) return NodeType.Function;
				else if( TYPE_EVENT.Equals( name ) ) return NodeType.Event;
				else if( TYPE_PROPERTY.Equals( name ) ) return NodeType.Property;
				else if( TYPE_CLASS.Equals( name ) ) return NodeType.Class;
				else if( TYPE_VAR.Equals( name ) ) return NodeType.Var;
				else if( TYPE_CONST.Equals( name ) ) return NodeType.Const;
				else return NodeType.Unknown;
			}
		}
		public ScriptNode() {
			TypeName = "unknown";
			Name = "";
			Arguments = new List<FunctionArg>();
			Comment = new ScriptComment();
			Members = new List<ScriptNode>();
		}

		[DataMember( Name = "type" )]
		public string TypeName { get; set; }

		[DataMember( Name = "name" )]
		public string Name { get; set; }

		[DataMember( Name = "arguments" )]
		public List<FunctionArg> Arguments { get; set; }

		[DataMember( Name = "returnType" )]
		public string ReturnType { get; set; }

		[DataMember( Name = "comment" )]
		public ScriptComment Comment { get; set; }

		[DataMember( Name = "members" )]
		public List<ScriptNode> Members { get; set; }

		/**
		 * ノードをマージする
		 * */
		public void margeNode( ScriptNode node ) {
			foreach( ScriptNode n in node.Members ) {
				string name = n.Name;
				int index = Members.FindIndex( s => s.Name == name );
				if( index >= 0 ) {
					Members[index].margeNode( n );
					if( n.Type == NodeType.Class && Members[index].Type == NodeType.Class ) {
						if( Members[index].Comment.Summary != null )
							Members[index].Comment.Summary += n.Comment.Summary;
						else
							Members[index].Comment.Summary = n.Comment.Summary;

						if( Members[index].Comment.Description != null )
							Members[index].Comment.Description += n.Comment.Description;
						else
							Members[index].Comment.Description = n.Comment.Description;

					}
				} else {
					Members.Add( n );
				}
			}
		}
		public void WriteHtml( ScriptNode parent, string owner, int constructorCount = 0 ) {
			switch( Type ) {
				case NodeType.Root:
				case NodeType.Unknown:
					// WriteRootHtml( ref writer );
					foreach( ScriptNode n in Members ) {
						n.WriteHtml( this, Name );
					}
					break;
				case NodeType.Function:
					WriteFunctionHtml( parent, owner, false, constructorCount );
					break;
				case NodeType.Event:
					WriteFunctionHtml( parent, owner, true );
					break;
				case NodeType.Property:
					WritePropertyHtml( owner );
					break;
				case NodeType.Class:
					WriteClassHtml( owner );
					int constructCount = 0;
					foreach( ScriptNode n in Members ) {
						if( n.Type == NodeType.Class ) {
							if( owner != null && owner.Length > 0 )
								n.WriteHtml( this, owner + "." + Name );
							else
								n.WriteHtml( this, Name );
						} else {
							n.WriteHtml( this, Name, constructCount );
							if( n.Type == NodeType.Function && n.Name == Name ) {
								constructCount++;
							}
						}
					}
					break;
			}
		}
		public void WriteMain( string title, string about ) {
			StreamWriter writer = new StreamWriter( "index.html", false, Encoding.UTF8 );
			WriteHtmlHeader( ref writer, title );
			writer.WriteLine( "<frameset cols=\"230,*\" title=\"index\">" );
			writer.WriteLine( "<frame src=\"frame_index.html\" name=\"index\" id=\"index\" title=\"インデックス\" />" );
			writer.WriteLine( "<frame src=\"about.html\" name=\"main\" id=\"main\" title=\"内容\" />" );
			writer.WriteLine( "<noframes><body>フレーム対応のブラウザでご覧ください</body></noframes>" );
			writer.WriteLine( "</frameset>" );
			writer.WriteLine( "</html>" );
			writer.Close();

			writer = new StreamWriter( "about.html", false, Encoding.UTF8 );
			WriteHtmlHeader( ref writer, title );
			writer.WriteLine( "<body>" );
			writer.WriteLine( Markdown.Transform( about ) );
			writer.WriteLine( "</body>" );
			writer.WriteLine( "</html>" );
			writer.Close();

			WriteIndex();

			WriteHtml( this, "" );
		}
		private void WriteIndex() {
			StreamWriter writer = new StreamWriter( "frame_index.html", false, Encoding.UTF8 );
			WriteHtmlHeader( ref writer, "目次" );
			writer.WriteLine( "<body>" );
			writer.WriteLine( "<h1><a id=\"id126\" name=\"id126\">クラスリファレンス</a></h1><div class=\"para\">" );

			List<ScriptNode> globalFunc = new List<ScriptNode>();
			Members.Sort( ( a, b ) => String.Compare( a.Name, b.Name ) );
			foreach( ScriptNode n in Members ) {
				if( n.Type == NodeType.Class ) {
					writer.WriteLine( "<a target=\"main\" class=\"jump\" href=\"class_" + n.Name + ".html\">" + n.Name + " クラス</a><br />" );
					n.Members.Sort( ( a, b ) => String.Compare( a.Name, b.Name ) );
					foreach( ScriptNode cn in n.Members ) {
						if( cn.Type == NodeType.Class ) {
							writer.WriteLine( "<a target=\"main\" class=\"jump\" href=\"class_" + n.Name + "." + cn.Name + ".html\">" + n.Name + "." + cn.Name + " クラス</a><br />" );
						}
					}
				}
			}
			writer.WriteLine( "</div>" );
			if( Members.FindIndex( s => s.Type == NodeType.Function ) >= 0 ) {
				writer.WriteLine( "<h1><a id=\"id127\" name=\"id127\">関数リファレンス</a></h1><div class=\"para\">" );
				foreach( ScriptNode n in Members ) {
					if( n.Type == NodeType.Function ) {
						writer.WriteLine( "<a target=\"main\" class=\"jump\" href=\"func__" + n.Name + ".html\">" + n.Name + " 関数</a><br />" );
					}
				}
				writer.WriteLine( "</div>" );
			}
			if( Members.FindIndex( s => s.Type == NodeType.Property ) >= 0 ) {
				writer.WriteLine( "<h1><a id=\"id127\" name=\"id127\">プロパティリファレンス</a></h1><div class=\"para\">" );
				foreach( ScriptNode n in Members ) {
					if( n.Type == NodeType.Property ) {
						writer.WriteLine( "<a target=\"main\" class=\"jump\" href=\"prop__" + n.Name + ".html\">" + n.Name + " プロパティ</a><br />" );
					}
				}
				writer.WriteLine( "</div>" );
			}
			if( Members.FindIndex( s => s.Type == NodeType.Const ) >= 0 ) {
				writer.WriteLine( "<h1><a id=\"id127\" name=\"id127\">定数リファレンス</a></h1><div class=\"para\">" );
				foreach( ScriptNode n in Members ) {
					if( n.Type == NodeType.Const ) {
						if( n.Comment != null )
							writer.WriteLine( n.Name + " (" + n.Comment.Summary + ")<br />" );
						else
							writer.WriteLine( n.Name + " ( )<br />" );
					}
				}
				writer.WriteLine( "</div>" );
			}

			writer.WriteLine( "</body>" );
			writer.WriteLine( "</html>" );
			writer.Close();
		}
		private string removeReturn( string str ) {
			if( str != null ) {
				str = str.Replace( "\\n", " " );
				str = str.Replace( "\n", " " );
			}
			return str;
		}
		private string returnToBr( string str ) {
			if( str != null ) {
				str = str.Replace( "\n", "<br />" );
				str = str.Replace( "\\n", "<br />" );
			}
			return str;
		}
		private string returnToCRLF( string str ) {
			if( str != null ) {
				str = str.Replace( "\\n", "\r\n" );
			}
			return str;
		}
		public int WriteClassHtml( string owner ) {
			int constructorCount = 0;
			foreach( ScriptNode n in Members ) {
				if( Name.Equals( n.Name ) ) {
					constructorCount++;
				}
			}
			Members.Sort( ( a, b ) => String.Compare(a.Name,b.Name) );

			string filename = "class_" + Name + ".html";
			if( owner != null && owner.Length > 0 ) {
				filename = "class_" + owner + "." + Name + ".html";
			}
			StreamWriter writer = new StreamWriter( filename, false, Encoding.UTF8 );
			WriteHtmlHeader( ref writer, Name + " - " + removeReturn(Comment.Summary) );
			writer.WriteLine( "<body>" );
			writer.WriteLine( "<h1><a name=\"top\" id=\"top\">" + Name + "</a></h1>" );
			writer.WriteLine( "<div class=\"para\">" + Markdown.Transform( returnToCRLF( Comment.Description ) ) + "</div>" );
			writer.WriteLine( "<h1>メンバ</h1><div class=\"para\">" );
			writer.WriteLine( "<dl>" );
			if( constructorCount > 0 ) {
				writer.WriteLine( "<dt>コンストラクタ</dt>" );
				writer.WriteLine( "<dd>" );
				var i = 0;
				foreach( ScriptNode n in Members ) {
					if( Name.Equals( n.Name ) ) {
						if( i != 0 ) {
							writer.WriteLine( "<a class=\"jump\" href=\"" + "func_" + Name + "_" + Name + "_" + i + ".html\">" + Name + "</a> (" + removeReturn( n.Comment.Summary ) + " )<br />" );
						} else {
							writer.WriteLine( "<a class=\"jump\" href=\"" + "func_" + Name + "_" + Name + ".html\">" + Name + "</a> (" + removeReturn( n.Comment.Summary ) + " )<br />" );
						}
						i++;
					}
				}
				writer.WriteLine( "</dd>" );
			}
			writer.WriteLine( "<dt>メソッド</dt>" );
			writer.WriteLine( "<dd>" );
			foreach( ScriptNode n in Members ) {
				if( n.Type == NodeType.Function && Name.Equals( n.Name ) == false ) {
					int index = 0;
					for( var i = 0; i < Members.Count; i++ ) {
						if( Members[i].Type == NodeType.Function && Members[i].Name.Equals( n.Name ) && Members[i] != n ) {
							index++;
						}
						if( Members[i] == n ) break;
					}
					if( index > 0 ) {
						writer.WriteLine( "<a class=\"jump\" href=\"" + "func_" + Name + "_" + n.Name + "_" + index + ".html\">" + n.Name + "</a> (" + removeReturn( n.Comment.Summary ) + " )<br />" );
					} else {
						writer.WriteLine( "<a class=\"jump\" href=\"" + "func_" + Name + "_" + n.Name + ".html\">" + n.Name + "</a> (" + removeReturn( n.Comment.Summary ) + " )<br />" );
					}
				}
			}
			writer.WriteLine( "</dd>" );
			writer.WriteLine( "<dt>プロパティ</dt>" );
			writer.WriteLine( "<dd>" );
			foreach( ScriptNode n in Members ) {
				if( n.Type == NodeType.Property ) {
					writer.WriteLine( "<a class=\"jump\" href=\"" + "prop_"+ Name + "_" + n.Name + ".html\">" + n.Name + "</a> (" + removeReturn( n.Comment.Summary ) + " )<br />" );
				}
			}
			writer.WriteLine( "</dd>" );
			writer.WriteLine( "<dt>イベント</dt>" );
			writer.WriteLine( "<dd>" );
			foreach( ScriptNode n in Members ) {
				if( n.Type == NodeType.Event ) {
					writer.WriteLine( "<a class=\"jump\" href=\"" + "event_" + Name + "_" + n.Name + ".html\">" + n.Name + "</a> (" + removeReturn( n.Comment.Summary ) + " )<br />" );
				}
			}
			if( Members.FindIndex( s => s.Type == NodeType.Const ) >= 0 ) {
				writer.WriteLine( "</dd>" );
				writer.WriteLine( "<dt>定数</dt>" );
				writer.WriteLine( "<dd>" );
				foreach( ScriptNode n in Members ) {
					if( n.Type == NodeType.Const ) {
						writer.WriteLine( n.Name + " (" + removeReturn( n.Comment.Summary ) + " )<br />" );
					}
				}
			}
			writer.WriteLine( "</dd>" );
			writer.WriteLine( "</dl>" );
			writer.WriteLine( "</div>" );
			writer.WriteLine( "</body>" );
			writer.WriteLine( "</html>" );
			writer.Close();
			return constructorCount;
		}
		public void WritePropertyHtml( string owner ) {
			StreamWriter writer = new StreamWriter( "prop_" + owner + "_" + Name + ".html", false, Encoding.UTF8 );
			WriteHtmlHeader( ref writer, Name + " - " + removeReturn(Comment.Summary) );
			writer.WriteLine( "<body>" );
			writer.WriteLine( "<h1><span class=\"fheader\"><a name=\"top\" id=\"top\">" + owner + "." + Name + "</a></span></h1><div class=\"para\">" );
			writer.WriteLine( "<dl>" );
			writer.WriteLine( "<dt>機能/意味</dt>" );
			writer.WriteLine( "<dd>" + removeReturn(Comment.Summary) + "</dd>" );
			writer.WriteLine( "<dt>タイプ</dt>" );
			if( owner != null && owner.Length > 0 )
				writer.WriteLine( "<dd><a class=\"jump\" href=\"" + "class_" + owner + ".html\">" + owner + "クラス</a>のプロパティ</dd>" ); // 読み出し専用かどうかのパースが必要か
			else
				writer.WriteLine( "<dd>グローバルプロパティ</dd>" );
			writer.WriteLine( "<dt>説明</dt>" );
			//writer.WriteLine( "<dd>" + returnToBr(Comment.Description) + "</dd>" );
			writer.WriteLine( "<dd>" + Markdown.Transform( returnToCRLF( Comment.Description ) ) + "</dd>" );
			if( Comment.See != null && Comment.See.Length > 0 ) {
				writer.WriteLine( "<dt>参照</dt>" );
				writer.WriteLine( "<dd>" + Markdown.Transform( returnToCRLF( Comment.See ) ) + "</dd>" );
			}
			writer.WriteLine( "</dl>" );
			writer.WriteLine( "</div>" );
			writer.WriteLine( "</body>" );
			writer.WriteLine( "</html>" );
			writer.Close();
		}
		public void WriteFunctionHtml( ScriptNode parent, string owner, bool ev = false, int index = 0 ) {
			StreamWriter writer;
			if( ev ) {
				writer = new StreamWriter( "event_" + owner + "_" + Name + ".html", false, Encoding.UTF8 );
			} else {
				if( parent.Name.Equals( Name ) ) {
					// constructor
					if( index > 0 ) {
						writer = new StreamWriter( "func_" + owner + "_" + Name + "_" + index + ".html", false, Encoding.UTF8 );
					} else {
						writer = new StreamWriter( "func_" + owner + "_" + Name + ".html", false, Encoding.UTF8 );
					}
				} else {
					int funcindex = 0;
					for( var i = 0; i < parent.Members.Count; i++ ) {
						if( parent.Members[i].Type == NodeType.Function && parent.Members[i].Name.Equals( Name ) && parent.Members[i] != this ) {
							funcindex++;
						}
						if( parent.Members[i] == this ) break;
					}
					if( funcindex > 0 ) {
						writer = new StreamWriter( "func_" + owner + "_" + Name + "_" + funcindex + ".html", false, Encoding.UTF8 );
					} else {
						writer = new StreamWriter( "func_" + owner + "_" + Name + ".html", false, Encoding.UTF8 );
					}
				}
			}
			WriteHtmlHeader( ref writer, Name + " - " + removeReturn(Comment.Summary) );
			writer.WriteLine( "<body>");
			writer.WriteLine( "<h1><span class=\"fheader\"><a name=\"top\" id=\"top\">" + owner + "." + Name + "</a></span></h1><div class=\"para\">" );
			writer.WriteLine( "<dl>" );
			writer.WriteLine( "<dt>機能/意味</dt>" );
			//writer.WriteLine( "<dd>" + removeReturn(Comment.Summary) + "</dd>" );
			writer.WriteLine( "<dd>" + Markdown.Transform( returnToCRLF(Comment.Summary) ) + "</dd>" );
			writer.WriteLine( "<dt>タイプ</dt>" );
			if( ev ) {
				if( owner != null && owner.Length > 0 )
					writer.WriteLine( "<dd><a class=\"jump\" href=\"" + "class_" + owner + ".html\">" + owner + "クラス</a>のイベント</dd>" );
				else
					writer.WriteLine( "<dd>グローバルイベント</dd>" );
			} else {
				if( owner != null && owner.Length > 0 )
					writer.WriteLine( "<dd><a class=\"jump\" href=\"" + "class_" + owner + ".html\">" + owner + "クラス</a>のメソッド</dd>" );
				else
					writer.WriteLine( "<dd>グローバルメソッド</dd>" );
			}
			writer.WriteLine( "<dt>構文</dt>" );
			writer.Write( "<dd><span class=\"funcdecl\">" + Name + "(" );
			if( Arguments != null ) {
				for( int i = 0; i < Arguments.Count; i++ ) {
					writer.Write( "<span class=\"arg\">" + Arguments[i].Name + "</span>" );
					if( Arguments[i].Type != null ) {
						writer.Write( ":" + Arguments[i].Type );
					}
					if( Arguments[i].Default != null ) {
						writer.Write( "<span class=\"defarg\">=<span class=\"defargval\">" + Arguments[i].Default + "</span></span>" );
					}
					if( ( i + 1 ) != Arguments.Count ) {
						writer.Write( ", " );
					}
				}
			}
			if( ReturnType != null ) {
				writer.WriteLine( ") :" + ReturnType + "</span></dd>" );
			} else {
				writer.WriteLine( ")</span></dd>" );
			}
			writer.WriteLine( "<dt>引数</dt>" );
			writer.WriteLine( "<dd><table rules=\"all\" frame=\"box\" cellpadding=\"3\" summary=\"" + Name + "の引数\">" );
			if( Comment.Params != null ) {
				for( int i = 0; i < Comment.Params.Count; i++ ) {
					FunctionParam p = Comment.Params[i];
					writer.WriteLine( "<tr><td valign=\"top\"><span class=\"argname\">" + p.Name + "</span></td>" );
					//writer.WriteLine( "<td>" + returnToBr(p.Description) + "</td></tr>" );
					writer.WriteLine( "<td>" + Markdown.Transform( returnToCRLF(p.Description) ) + "</td></tr>" );
				}
			}
			writer.WriteLine( "</table></dd>" );
			writer.WriteLine( "<dt>戻り値</dt>" );
			if( Comment.Return != null && Comment.Return.Length > 0 ) {
				writer.WriteLine( "<dd>" + Markdown.Transform( returnToCRLF(Comment.Return) ) + "</dd>" );
			} else {
				writer.WriteLine( "<dd>なし (void)</dd>" );
			}
			writer.WriteLine( "<dt>説明</dt>" );
			//writer.WriteLine( "<dd>" + returnToBr(Comment.Description) + "</dd>" );
			writer.WriteLine( "<dd>" + Markdown.Transform( returnToCRLF( Comment.Description ) ) + "</dd>" );
			if( Comment.See != null && Comment.See.Length > 0 ) {
				writer.WriteLine( "<dt>参照</dt>" );
				writer.WriteLine( "<dd>" + Markdown.Transform( returnToCRLF( Comment.See)  ) + "</dd>" );
			}
			writer.WriteLine( "</dl>" );
			writer.WriteLine( "</div>" );
			writer.WriteLine( "</body>" );
			writer.WriteLine( "</html>" );
			writer.Close();
		}
		private void WriteHtmlHeader( ref StreamWriter writer, string title ) {
			writer.WriteLine( "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" );
			writer.WriteLine( "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">" );
			writer.WriteLine( "<html><head><meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />" );
			writer.WriteLine( "<link href=\"api.css\" type=\"text/css\" rel=\"stylesheet\" title=\"APIリファレンス用標準スタイル\" />" );
			writer.WriteLine( "<title>" + title + "</title>" );
			writer.WriteLine( "</head>" );
		}
	}
}
