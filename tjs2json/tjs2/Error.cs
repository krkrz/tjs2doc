using System;
using System.Collections.Generic;
using System.Text;

namespace TJS2 {
	sealed class Error {
		/*
		public string UnclosedComment { get { return "コメントが閉じられていません"; } }
		public string StringParseError { get { return "不正な文字列です"; } }
		public string PPError { get { return "不正なプリプロセッサです"; } }
		public string NumberError { get { return "不正な数値形式です"; } }
		public string InvalidChar { get { return "%1 は不正な文字です"; } }
		public string EndOfBlockError { get { return "ブロックの対応が取れていません。\"}\"が多いです"; } }
		public string NotFoundBlockRBRACEError { get { return "ブロックの終わりに\"}\"がありません"; } }
		*/
		public static string RegexParseError { get { return "不正な正規表現です"; } }
		public static string OctetParseError { get { return "不正なOctetです"; } }
		
		public const string InternalError = "内部エラーが発生しました";
		public const string Warning = "警告: ";
		public const string WarnEvalOperator = "グローバルでない場所で後置 ! 演算子が使われています(この演算子の挙動はTJS2 version 2.4.1 で変わりましたのでご注意ください)";
		public const string NarrowToWideConversionError = "ANSI 文字列を UNICODE 文字列に変換できません。現在のコードページで解釈できない文字が含まれてます。正しいデータが指定されているかを確認してください。データが破損している可能性もあります";
		public const string VariantConvertError = "%1 から %2 へ型を変換できません";
		public const string VariantConvertErrorToObject = "%1 から Object へ型を変換できません。Object 型が要求される文脈で Object 型以外の値が渡されるとこのエラーが発生します";
		public const string IDExpected = "識別子を指定してください";
		public const string SubstitutionInBooleanContext = "論理値が求められている場所で = 演算子が使用されています(== 演算子の間違いですか？代入した上でゼロと値を比較したい場合は、(A=B) != 0 の形式を使うことをお勧めします)";
		public const string CannotModifyLHS = "不正な代入か不正な式の操作です";
		public const string InsufficientMem = "メモリが足りません";
		public const string CannotGetResult = "この式からは値を得ることができません";
		public const string NullAccess = "null オブジェクトにアクセスしようとしました";
		public const string MemberNotFound = "メンバ \"%1\" が見つかりません";
		public const string MemberNotFoundNoNameGiven = "メンバが見つかりません";
		public const string NotImplemented = "呼び出そうとした機能は未実装です";
		public const string InvalidParam = "不正な引数です";
		public const string BadParamCount = "引数の数が不正です";
		public const string InvalidType = "関数ではないかプロパティの種類が違います";
		public const string SpecifyDicOrArray = "Dictionary または Array クラスのオブジェクトを指定してください";
		public const string SpecifyArray = "Array クラスのオブジェクトを指定してください";
		public const string StringDeallocError = "文字列メモリブロックを解放できません";
		public const string StringAllocError = "文字列メモリブロックを確保できません";
		public const string MisplacedBreakContinue = "\"break\" または \"continue\" はここに書くことはできません";
		public const string MisplacedCase = "\"case\" はここに書くことはできません";
		public const string MisplacedReturn = "\"return\" はここに書くことはできません";
		public const string StringParseError = "文字列定数/正規表現/オクテット即値が終わらないままスクリプトの終端に達しました";
		public const string NumberError = "数値として解釈できません";
		public const string UnclosedComment = "コメントが終わらないままスクリプトの終端に達しました";
		public const string InvalidChar = "不正な文字です : \'%1\'";
		public const string Expected = "%1 がありません";
		public const string SyntaxError = "文法エラーです(%1)";
		public const string PPError = "条件コンパイル式にエラーがあります";
		public const string CannotGetSuper = "スーパークラスが存在しないかスーパークラスを特定できません";
		public const string InvalidOpecode = "不正な VM コードです";
		public const string RangeError = "値が範囲外です";
		public const string AccessDenyed = "読み込み専用あるいは書き込み専用プロパティに対して行えない操作をしようとしました";
		public const string NativeClassCrash = "実行コンテキストが違います";
		public const string InvalidObject = "オブジェクトはすでに無効化されています";
		public const string CannotOmit = "\"...\" は関数外では使えません";
		public const string CannotParseDate = "不正な日付文字列の形式です";
		public const string InvalidValueForTimestamp = "不正な日付・時刻です";
		public const string ExceptionNotFound = "\"Exception\" が存在しないため例外オブジェクトを作成できません";
		public const string InvalidFormatString = "不正な書式文字列です";
		public const string DivideByZero = "0 で除算をしようとしました";
		public const string NotReconstructiveRandomizeData = "乱数系列を初期化できません(おそらく不正なデータが渡されました)";
		public const string Symbol = "識別子";
		public const string CallHistoryIsFromOutOfTJS2Script = "[TJSスクリプト管理外]";
		public const string NObjectsWasNotFreed = "合計 %1 個のオブジェクトが解放されていません";
		public const string ObjectCreationHistoryDelimiter = "\n                     ";
		public const string ObjectWasNotFreed = "オブジェクト %1 [%2] が解放されていません。オブジェクト作成時の呼び出し履歴は以下の通りです:\n                     %3";
		public const string GroupByObjectTypeAndHistory = "オブジェクトのタイプとオブジェクト作成時の履歴による分類";
		public const string GroupByObjectType = "オブジェクトのタイプによる分類";
		public const string ObjectCountingMessageGroupByObjectTypeAndHistory = "%1 個 : [%2]\n                     %3";
		public const string ObjectCountingMessageTJSGroupByObjectType = "%1 個 : [%2]";
		public const string WarnRunningCodeOnDeletingObject = "%4: 削除中のオブジェクト %1[%2] 上でコードが実行されています。このオブジェクトの作成時の呼び出し履歴は以下の通りです:\n                     %3";
		public const string WriteError = "書き込みエラーが発生しました";
		public const string ReadError = "読み込みエラーが発生しました。ファイルが破損している可能性や、デバイスからの読み込みに失敗した可能性があります";
		public const string SeekError = "シークエラーが発生しました。ファイルが破損している可能性や、デバイスからの読み込みに失敗した可能性があります";

		public const string TooManyErrors = "Too many errors";
		public const string ConstDicDelimiterError = "定数辞書(const Dictionary)で要素名と値の区切りが不正です";
		public const string ConstDicValueError = "定数辞書(const Dictionary)の要素値が不正です";
		public const string ConstArrayValueError = "定数配列(const Array)の要素値が不正です";
		public const string ConstDicArrayStringError = "定数辞書もしくは配列で(const)文字が不正です";
		public const string ConstDicLBRACKETError = "定数辞書(const Dictionary)で(const)%の後に\"[\"がありません";
		public const string ConstArrayLBRACKETError = "定数配列(const Array)で(const)の後に\"[\"がありません";
		public const string DicDelimiterError = "辞書(Dictionary)で要素名と値の区切りが不正です";
		public const string DicError = "辞書(Dictionary)が不正です";
		public const string DicLBRACKETError = "辞書(Dictionary)で%の後に\"[\"がありません";
		public const string DicRBRACKETError = "辞書(Dictionary)の終端に\"]\"がありません";
		public const string ArrayRBRACKETError = "配列(Array)の終端に\"]\"がありません";
		public const string NotFoundRegexError = "正規表現が要求される文脈で正規表現がありません";
		public const string NotFoundSymbolAfterDotError = "\".\"の後にシンボルがありません";
		public const string NotFoundDicOrArrayRBRACKETError = "配列もしくは辞書要素を指す変数の終端に\"]\"がありません";
		public const string NotFoundRPARENTHESISError = "\")\"が要求される文脈で\")\"がありません";
		public const string NotFoundSemicolonAfterThrowError = "throwの後の\";\"がありません";
		public const string NotFoundRPARENTHESISAfterCatchError= "catchの後の\")\"がありません";
		public const string NotFoundCaseOrDefaultError = "caseかdefaultが要求される文脈でcaseかdefaultがありません";
		public const string NotFoundWithLPARENTHESISError = "withの後に\"(\"がありません";
		public const string NotFoundWithRPARENTHESISError = "withの後に\")\"がありません";
		public const string NotFoundSwitchLPARENTHESISError = "switchの後に\"(\"がありません";
		public const string NotFoundSwitchRPARENTHESISError = "switchの後に\")\"がありません";
		public const string NotFoundSemicolonAfterReturnError = "returnの後の\";\"がありません";
		public const string NotFoundPropGetRPARENTHESISError = "property getterの後に\")\"がありません";
		public const string NotFoundPropSetLPARENTHESISError = "property setterの後に\"(\"がありません";
		public const string NotFoundPropSetRPARENTHESISError = "property setterの後に\")\"がありません";
		public const string NotFoundPropError = "propertyの後に\"getter\"もしくは\"setter\"がありません";
		public const string NotFoundSymbolAfterPropError = "propertyの後にシンボルがありません";
		public const string NotFoundLBRACEAfterPropError = "propertyの後に\"{\"がありません";
		public const string NotFoundRBRACEAfterPropError = "propertyの後に\"}\"がありません";
		public const string NotFoundFuncDeclRPARENTHESISError = "関数定義の後に\")\"がありません";
		public const string NotFoundFuncDeclSymbolError = "関数定義にシンボル名がありません";
		public const string NotFoundSymbolAfterVarError = "変数宣言にシンボルがありません";
		public const string NotFoundForLPARENTHESISError = "forの後に\"(\"がありません";
		public const string NotFoundForRPARENTHESISError = "forの後に\")\"がありません";
		public const string NotFoundForSemicolonError = "forの各節の区切りに\";\"がありません";
		public const string NotFoundIfLPARENTHESISError = "ifの後に\"(\"がありません";
		public const string NotFoundIfRPARENTHESISError = "ifの後に\")\"がありません";
		public const string NotFoundDoWhileLPARENTHESISError = "do-whileの後に\"(\"がありません";
		public const string NotFoundDoWhileRPARENTHESISError = "do-whileの後に\")\"がありません";
		public const string NotFoundDoWhileError = "do-while文でwhileがありません";
		public const string NotFoundDoWhileSemicolonError = "do-while文でwhileの後に\";\"がありません";
		public const string NotFoundWhileLPARENTHESISError = "whileの後に\"(\"がありません";
		public const string NotFoundWhileRPARENTHESISError = "whileの後に\")\"がありません";
		public const string NotFoundLBRACEAfterBlockError = "ブロックが要求される文脈で\"{\"がありません";
		public const string NotFoundRBRACEAfterBlockError = "ブロックが要求される文脈で\"}\"がありません";
		public const string NotFoundSemicolonError = "文の終わりに\";\"がありません";
		public const string NotFoundSemicolonOrTokenTypeError = "文の終わりに\";\"がないか、予約語のタイプミスです";
		public const string NotFoundBlockRBRACEError = "ブロックの終わりに\"}\"がありません";
		public const string NotFoundCatchError = "tryの後にcatchがありません";
		public const string NotFoundFuncCallLPARENTHESISError = "関数呼び出しの後に\"(\"がありません";
		public const string NotFoundFuncCallRPARENTHESISError = "関数呼び出しの後に\")\"がありません";
		public const string NotFoundVarSemicolonError = "変数宣言の後に\";\"がありません";
		public const string NotFound3ColonError = "条件演算子の\":\"がありません";
		public const string NotFoundCaseColonError = "caseの後に\":\"がありません";
		public const string NotFoundDefaultColonError = "defaultの後に\":\"がありません";
		public const string NotFoundSymbolAfterClassError = "classの後にシンボルがありません";
		public const string NotFoundPropSetSymbolError = "property setterの引数がありません";
		public const string NotFoundBreakSemicolonError = "breakの後に\";\"がありません";
		public const string NotFoundContinueSemicolonError = "continueの後に\";\"がありません";
		public const string NotFoundBebuggerSemicolonError = "debuggerの後に\";\"がありません";
		public const string NotFoundAsteriskAfterError = "関数呼び出し、関数定義の配列展開(*)が不正か、乗算が不正です";
		public const string EndOfBlockError = "ブロックの対応が取れていません。\"}\"が多いです";
		public const string NotFoundType = "型名がありません";

		public const string NotFoundPreprocessorRPARENTHESISError = "プリプロセッサに\")\"がありません";
		public const string PreprocessorZeroDiv = "プリプロセッサのゼロ除算エラー";

		public const string ByteCodeBroken = "バイトコードファイル読み込みエラー。ファイルが壊れているかバイトコードとは異なるファイルです";

	}
}
