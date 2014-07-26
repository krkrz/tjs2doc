using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TJS2Json {
	class Program {
		static void Main( string[] args ) {
			if( args.Length < 2 ) {
				System.Console.WriteLine("tjs2json [tjs2 file name] [output json file name]");
				return;
			}
			TJS2.Parser parser = new TJS2.Parser();
			string encoding = "UTF-8";
			if( args.Length >= 3 ) {
				encoding = args[2];
			}
			parser.toJson( args[0], args[1], encoding );
		}
	}
}
