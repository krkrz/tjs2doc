using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace apijsontohtml {
	class Program {
		static void Main( string[] args ) {
			if( args.Length > 0 ) {
				string input = args[0];
				string title = "タイトル";
				if( args.Length > 1 ) {
					title = args[1];
				}
				string regptn = Regex.Escape( input );
				regptn = regptn.Replace( @"\*", ".*?" );
				regptn = regptn.Replace( @"\?", "." );

				ScriptNode root = new ScriptNode();
				Regex regex = new Regex( regptn );
				foreach( string s in Directory.GetFiles( ".\\" ) ) {
					if( regex.IsMatch( s ) ) {
						Stream stream = null;
						try {
							stream = new FileStream( s, FileMode.Open );
							var serializer = new DataContractJsonSerializer( typeof( ScriptNode ) );
							ScriptNode node = (ScriptNode)serializer.ReadObject( stream );
							root.margeNode( node );
						} catch( Exception ) {
							if( stream != null ) stream.Close();
						}
					}
				}
				string about = title;
				if( System.IO.File.Exists( "about.md" ) ) {
					System.IO.StreamReader sr = new System.IO.StreamReader( "about.md", System.Text.Encoding.GetEncoding( "UTF-8" ) );
					about = sr.ReadToEnd();
					sr.Close();
				}
				if( root.Members != null && root.Members.Count > 0 ) {
					var option = new MarkdownSharp.MarkdownOptions();
					option.AutoHyperlink = true;
					option.AutoNewLines = true;
					option.EmptyElementSuffix = " />";
					option.EncodeProblemUrlCharacters = true;
					option.LinkEmails = true;
					option.StrictBoldItalic = false;
					ScriptNode.Markdown = new MarkdownSharp.Markdown( option );
					ScriptNode.Root = root;
					root.WriteMain( title, about );
				}
				/*
				Stream stream = new FileStream( args[0], FileMode.Open );
				var serializer = new DataContractJsonSerializer( typeof( ScriptNode ) );
				try {
					ScriptNode node = (ScriptNode)serializer.ReadObject( stream );
					node.WriteHtml( "" );
				} catch( System.Runtime.Serialization.SerializationException e ) {
					System.Console.WriteLine( e.Message );
					System.Console.WriteLine( e.Source );
				}
				stream.Close();
				*/
			} else {
				System.Console.WriteLine( "apijsontohtml [json file name]" );
			}
		}
	}
}
