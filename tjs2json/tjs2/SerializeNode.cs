using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Text.RegularExpressions;

namespace TJS2 {
	[DataContract]
	class FunctionParam {
		[DataMember( Name = "name" )]
		public string Name { get; set; }

		[DataMember( Name = "desc" )]
		public string Description { get; set; }

		public FunctionParam() {
			Name = Description = null;
		}
		public FunctionParam( string name, string desc ) {
			Name = name;
			Description = desc;
		}
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
			Raw = Summary = Return = Throw = Author = Version = See = Description = Unknown = null;
			Params = new List<FunctionParam>();
		}
	}
	[DataContract]
	class SerializeNode {
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
		public const string TYPE_EVENT = "event";
		public const string TYPE_PROPERTY = "property";
		public const string TYPE_CLASS = "class";
		public const string TYPE_VAR = "var";
		public const string TYPE_CONST = "const";
		public const string TYPE_UNKNOWN = "unknown";

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
			set {
				switch( value ) {
					case NodeType.Root:
						TypeName = TYPE_ROOT;
						break;
					case NodeType.Function:
						TypeName = TYPE_FUNCTION;
						break;
					case NodeType.Event:
						TypeName = TYPE_EVENT;
						break;
					case NodeType.Property:
						TypeName = TYPE_PROPERTY;
						break;
					case NodeType.Class:
						TypeName = TYPE_CLASS;
						break;
					case NodeType.Var:
						TypeName = TYPE_VAR;
						break;
					case NodeType.Const:
						TypeName = TYPE_CONST;
						break;
					case NodeType.Unknown:
					default:
						TypeName = TYPE_UNKNOWN;
						break;
				}
			}
		}
		public SerializeNode() {
			TypeName = "unknown";
			Name = null;
			Arguments = new List<string>();
			Comment = new ScriptComment();
			Members = new List<SerializeNode>();
		}

		[DataMember( Name = "type" )]
		public string TypeName { get; set; }

		[DataMember( Name = "name" )]
		public string Name { get; set; }

		[DataMember( Name = "arguments" )]
		public List<string> Arguments { get; set; }

		[DataMember( Name = "comment" )]
		public ScriptComment Comment { get; set; }

		[DataMember( Name = "members" )]
		public List<SerializeNode> Members { get; set; }

		static public SerializeNode translateNode( ref SerializeNode parent, Parser.ScriptNode node ) {
			SerializeNode sn = new SerializeNode();
			if( parent != null ) parent.Members.Add( sn );
			switch( node.Type ) {
				case 0:
					sn.Type = NodeType.Root;
					break;
				case Token.T_FUNCTION:
					sn.Type = NodeType.Function;
					break;
				case Token.T_EVENT:
					sn.Type = NodeType.Event;
					break;
				case Token.T_PROPERTY:
					sn.Type = NodeType.Property;
					break;
				case Token.T_CLASS:
					sn.Type = NodeType.Class;
					break;
				case Token.T_VAR:
					sn.Type = NodeType.Var;
					break;
				case Token.T_CONST:
					sn.Type = NodeType.Const;
					break;
				default:
					sn.Type = NodeType.Unknown;
					break;
			}
			sn.Name = node.Name;
			if( node.Args != null && node.Args.Count > 0 ) {
				foreach( string name in node.Args ) {
					sn.Arguments.Add( name );
				}
			}
			string comment = node.Comment;
			if( comment != null ) {
				List<Parser.CommentNode> cnl = Parser.parseComment( comment );
				for( int i = 0; i < cnl.Count; i++ ) {
					Parser.CommentNode cn = cnl[i];
					string body = Regex.Replace( cn.Body, "[ \t]*\\\\n$", "" );	// 末尾の空白と改行は削除する
					switch( cn.Type ) {
						case Parser.CommentType.SUMMARY:
							sn.Comment.Summary = body;
							break;
						case Parser.CommentType.PARAM:
							sn.Comment.Params.Add( new FunctionParam(cn.Name,body) );
							break;
						case Parser.CommentType.RETURN:
							sn.Comment.Return = body;
							break;
						case Parser.CommentType.THROW:
							sn.Comment.Throw = body;
							break;
						case Parser.CommentType.AUTHOR:
							sn.Comment.Author = body;
							break;
						case Parser.CommentType.VERSION:
							sn.Comment.Version = body;
							break;
						case Parser.CommentType.SEE:
							sn.Comment.See = body;
							break;
						case Parser.CommentType.DESCRIPTION:
							sn.Comment.Description = body;
							break;
						default:
							sn.Comment.Unknown = body;
							break;
					}
				}
			}
			if( node.Child != null && node.Child.Count > 0 ) {
				foreach( Parser.ScriptNode c in node.Child ) {
					translateNode( ref sn, c );
				}
			}
			return sn;
		}
	}
}
