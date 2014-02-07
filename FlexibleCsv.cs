#region License

/*-----------------------------------------------------------------------------
Version: 1.0.3.0
Blog: http://oninoiori.blog72.fc2.com/
DL Site: http://www48.tok2.com/home/oninonando/
Author: Ao-Oni <ao-oni@mail.goo.ne.jp>
Licensed under The MIT License
Redistributions of files must retain the above copyright notice.
-----------------------------------------------------------------------------*/

#endregion

#region FlexibleCsv

#region using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

#endregion

namespace AoOni
{
	#region Main Class
	
	internal static class FlexibleCsv
	{
		#region Definitions
		
		#region XPath

		private const string XPATH_VARIABLE_ELEMENT = "variableList/variable";
		private const string XPATH_COL_ELEMENT = "colList/col";
		private const string XPATH_PARAM_ELEMENT = "paramList/param";

		#endregion

		#region Type Cache

		private static Type _flexibleCsv = typeof(FlexibleCsv);
		private static Type _objectType = typeof(object);
		private static Type _intType = typeof(int);
		private static Type _longType = typeof(long);
		private static Type _decimalType = typeof(decimal);
		private static Type _doubleType = typeof(double);
		private static Type _boolType = typeof(bool);
		private static Type _dateTimeType = typeof(DateTime);
		private static Type _timeSpanType = typeof(TimeSpan);
		private static Type _stringType = typeof(string);
		private static Type _arrayType = typeof(Array);
		private static Type _regexType = typeof(Regex);

		#endregion

		#region Type List

		private static Dictionary<string, Type> _typeList = new Dictionary<string, Type>()
			{
				{"object", _objectType},
				{"int", _intType},
				{"long", _longType},
				{"decimal", _decimalType},
				{"double", _doubleType},
				{"bool", _boolType},
				{"datetime", _dateTimeType},
				{"string", _stringType},
			};

		private const string TYPE_NAME_STRING = "string";

		#endregion

		#region Element Name

		private const string ELEMENT_NAME_FILTER = "filter";
		private const string ELEMENT_NAME_CAST = "cast";
		private const string ELEMENT_NAME_PARSE = "parse";
		private const string ELEMENT_NAME_TYPE = "type";
		private const string ELEMENT_NAME_ARRAYITEMLIST = "itemList";
		private const string ELEMENT_NAME_ARRAYITEM = "item";
		private const string ELEMENT_NAME_METHOD = "method";
		private const string ELEMENT_NAME_CONDITION = "condition";
		private const string ELEMENT_NAME_MATCH = "match";
		private const string ELEMENT_NAME_MISMATCH = "mismatch";
		private const string ELEMENT_NAME_INSTANCE = "instance";
		private const string ELEMENT_NAME_VARIABLENAME = "name";
		private const string ELEMENT_NAME_ASSIGN = "assign";

		#endregion

		#region String Instance Method / Property

		private static MethodInfo _stringReplaceMethod = _stringType.GetMethod("Replace", new Type[] { _stringType, _stringType });
		private static MethodInfo _stringSubstringMethod1 = _stringType.GetMethod("Substring", new Type[] { _intType });
		private static MethodInfo _stringSubstringMethod2 = _stringType.GetMethod("Substring", new Type[] { _intType, _intType });
		private static MethodInfo _stringIndexOfMethod = _stringType.GetMethod("IndexOf", new Type[] { _stringType });
		private static MethodInfo _stringSplitMethod = _stringType.GetMethod("Split", new Type[] { _stringType.MakeArrayType(), typeof(StringSplitOptions) });
		private static PropertyInfo _stringLengthProperty = _stringType.GetProperty("Length");

		private static MethodInfo _regexIsMatchMethod = _regexType.GetMethod("IsMatch", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType, _stringType }, null);
		private static MethodInfo _regexReplaceMethod = _regexType.GetMethod("Replace", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType, _stringType, _stringType }, null);

		private static Dictionary<string, Func<Expression, Expression[], Expression>> _stringMethodExpressionList = new Dictionary<string, Func<Expression, Expression[], Expression>>()
		{
			{"replace", (instance, paramList) => Expression.Call(instance, _stringReplaceMethod, Expression.Call(paramList[0], _toStringMethod), Expression.Call(paramList[1], _toStringMethod))},
			{"substr", (instance, paramList) => (paramList.Count() == 1) ? Expression.Call(instance, _stringSubstringMethod1, paramList[0]) : Expression.Call(instance, _stringSubstringMethod2, paramList[0], paramList[1])},
			{"indexOf", (instance, paramList) => Expression.Call(instance, _stringIndexOfMethod, Expression.Call(paramList[0], _toStringMethod))},
			{"split", (instance, paramList) => Expression.Call(instance, _stringSplitMethod, Expression.NewArrayInit(_stringType, Expression.Call(paramList[0], _toStringMethod)), Expression.Constant(StringSplitOptions.None))},
			{"length", (instance, dummy) => Expression.Property(instance, _stringLengthProperty)},
			{"isMatch", (instance, paramList) => Expression.Call(_regexIsMatchMethod, instance, Expression.Call(paramList[0], _toStringMethod))},
			{"regReplace", (instance, paramList) => Expression.Call(_regexReplaceMethod, instance, Expression.Call(paramList[0], _toStringMethod), Expression.Call(paramList[1], _toStringMethod))},
		};

		#endregion

		#region Array Method

		private static MethodInfo _arrayIndexOfMethod = _arrayType.GetMethod("IndexOf", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _arrayType, _objectType }, null);
		private static MethodInfo _arraySetArrayItemValueMethod = _flexibleCsv.GetMethod("SetArrayItemValue", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { _arrayType, _objectType, _intType }, null);

		private static Dictionary<string, Func<Expression, Expression[], Expression>> _arrayMethodExpressionList = new Dictionary<string, Func<Expression, Expression[], Expression>>()
		{
			{"getValue", (instance, paramList) => Expression.ArrayAccess(instance, paramList[0])},
			{"setValue", (instance, paramList) => Expression.Call(_arraySetArrayItemValueMethod, instance, paramList[0], paramList[1])},
			{"indexOf", (instance, paramList) => Expression.Call(_arrayIndexOfMethod, instance, paramList[0])},
			{"length", (instance, dummy) => Expression.ArrayLength(instance)},
		};

		#endregion

		#region DateTime Method / TimeSpan Property

		private static MethodInfo _dateTimeAddDaysMethod = _dateTimeType.GetMethod("AddDays");
		private static MethodInfo _dateTimeAddHoursMethod = _dateTimeType.GetMethod("AddHours");
		private static MethodInfo _dateTimeAddMinutesMethod = _dateTimeType.GetMethod("AddMinutes");
		private static MethodInfo _dateTimeAddSecondsMethod = _dateTimeType.GetMethod("AddSeconds");
		private static MethodInfo _dateTimeToStringMethod1 = _dateTimeType.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, new Type[]{}, null);
		private static MethodInfo _dateTimeToStringMethod2 = _dateTimeType.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { _stringType }, null);

		private static PropertyInfo _timeSpanDaysProperty = _timeSpanType.GetProperty("Days");
		private static PropertyInfo _timeSpanHoursProperty = _timeSpanType.GetProperty("Hours");
		private static PropertyInfo _timeSpanMinutesProperty = _timeSpanType.GetProperty("Minutes");
		private static PropertyInfo _timeSpanSecondsProperty = _timeSpanType.GetProperty("Seconds");

		private static Dictionary<string, Func<Expression, Expression[], Expression>> _dateTimeMethodExpressionList = new Dictionary<string, Func<Expression, Expression[], Expression>>()
		{
			{"addDays", (instance, paramList) => Expression.Call(instance, _dateTimeAddDaysMethod, Expression.Convert(paramList[0], _doubleType))},
			{"addHours", (instance, paramList) => Expression.Call(instance, _dateTimeAddHoursMethod, Expression.Convert(paramList[0], _doubleType))},
			{"addMinutes", (instance, paramList) => Expression.Call(instance, _dateTimeAddMinutesMethod, Expression.Convert(paramList[0], _doubleType))},
			{"addSeconds", (instance, paramList) => Expression.Call(instance, _dateTimeAddSecondsMethod, Expression.Convert(paramList[0], _doubleType))},
			{"diffDays", (instance, paramList) => Expression.Property(Expression.Subtract(instance, paramList[0]), _timeSpanDaysProperty)},
			{"diffHours", (instance, paramList) => Expression.Property(Expression.Subtract(instance, paramList[0]), _timeSpanHoursProperty)},
			{"diffMinutes", (instance, paramList) => Expression.Property(Expression.Subtract(instance, paramList[0]), _timeSpanMinutesProperty)},
			{"diffSeconds", (instance, paramList) => Expression.Property(Expression.Subtract(instance, paramList[0]), _timeSpanSecondsProperty)},
			{"toString", (instance, paramList) => paramList.Length == 0 ? Expression.Call(instance, _dateTimeToStringMethod1) : Expression.Call(instance, _dateTimeToStringMethod2, paramList[0])},
		};
	
		#endregion

		#region Create Expression Delegate

		private static Func<XElement, Expression> _createStringExpressionDelegate = CreateInstanceExpression(_stringMethodExpressionList, true);
		private static Func<XElement, Expression> _createArrayExpressionDelegate = CreateInstanceExpression(_arrayMethodExpressionList, false);
		private static Func<XElement, Expression> _createDateTimeExpressionDelegate = CreateInstanceExpression(_dateTimeMethodExpressionList, false);

		private static Dictionary<XName, Func<XElement, Expression>> _createExpressionDelegateList = new Dictionary<XName, Func<XElement, Expression>>()
			{
				{"source", CreateSourceExpression},
				{"arrayInit", CreateArrayInitExpression},
				{"calculate", CreateCalculateExpression},
				{"if", CreateConditionalExpression},
				{"variable", CreateVariableExpression},
				{"string", _createStringExpressionDelegate},
				{"array", _createArrayExpressionDelegate},
				{"datetime", _createDateTimeExpressionDelegate},
			};

		#endregion

		#region Source Expression

		private static Dictionary<XName, Func<string, Expression>> _sourceExpressionList = new Dictionary<XName, Func<string, Expression>>()
			{
				{"csv", elementValue => Expression.ArrayIndex(_csv, Expression.Constant(int.Parse(elementValue) - 1, _intType))},
				{"const", elementValue => Expression.Constant(elementValue)},
				{"rowIndex", dummy => _rowIndex},
				{"colIndex", dummy => _colIndex},
				{"now", dummy => Expression.Constant(DateTime.Now)},
				{"today", dummy => Expression.Constant(DateTime.Today)},
			};
		
		#endregion

		#region Calculation Method

		private static MethodInfo _toStringMethod = _objectType.GetMethod("ToString");

		private static MethodInfo _stringConcatMethod = _stringType.GetMethod("Concat", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType, _stringType }, null);
		private static MethodInfo _stringFormatMethod1 = _stringType.GetMethod("Format", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType, _objectType.MakeArrayType() }, null);
		private static MethodInfo _stringFormatMethod2 = _stringType.GetMethod("Format", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType, _objectType }, null);
		private static MethodInfo _stringJoinMethod = _stringType.GetMethod("Join", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType, _stringType.MakeArrayType() }, null);
		private static MethodInfo _convertToStringArrayMethod = _flexibleCsv.GetMethod("ConvertToStringArray", BindingFlags.NonPublic | BindingFlags.Static);
	
		private static Dictionary<string, Func<Expression[],  Expression>> _stringStaticMethodExpressionList = new Dictionary<string, Func<Expression[], Expression>>()
		{
			{".", (paramList) => Expression.Call(_stringConcatMethod, Expression.Call(paramList[0], _toStringMethod), Expression.Call(paramList[1], _toStringMethod))},
			{"format", (paramList) => Expression.Call((paramList[1] is NewArrayExpression) ? _stringFormatMethod1 : _stringFormatMethod2, Expression.Call(paramList[0], _toStringMethod), paramList[1])},
			{"join", (paramList) => Expression.Call(_stringJoinMethod, Expression.Call(paramList[0], _toStringMethod), Expression.Call(_convertToStringArrayMethod, paramList[1]))},
		};

		private static Dictionary<string, ExpressionType> _calculateExpressionTypeList = new Dictionary<string, ExpressionType>()
			{
				{"+", ExpressionType.Add},
				{"-", ExpressionType.Subtract},
				{"*", ExpressionType.Multiply},
				{"/", ExpressionType.Divide},
				{"pow", ExpressionType.Power},
				{"mod", ExpressionType.Modulo},
				{"=", ExpressionType.Equal},
				{"!=", ExpressionType.NotEqual},
				{"and", ExpressionType.And},
				{"or", ExpressionType.Or},
				{"xor", ExpressionType.ExclusiveOr},
				{"gt", ExpressionType.GreaterThan},
				{"ge", ExpressionType.GreaterThanOrEqual},
				{"lt", ExpressionType.LessThan},
				{"le", ExpressionType.LessThanOrEqual},
			};

		#endregion

		#region Parse Method

		private static Dictionary<string, MethodInfo> _parseMethodList = new Dictionary<string, MethodInfo>()
			{
				{"int", _intType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType }, null)},
				{"long", _longType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType }, null)},
				{"decimal", _decimalType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType }, null)},
				{"double", _doubleType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType }, null)},
				{"bool", _boolType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType }, null)},
				{"datetime", _dateTimeType.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, null, new Type[] { _stringType }, null)},
			};

		#endregion

		#region Parameter Expression

		private static ParameterExpression _csv = Expression.Parameter(typeof(string[]));
		private static ParameterExpression _rowIndex = Expression.Parameter(_longType);
		private static ParameterExpression _colIndex = Expression.Parameter(_longType);

		#endregion

		#region Variables Expression

		private static Dictionary<string, ParameterExpression> _variableList = new Dictionary<string, ParameterExpression>();
		private static List<Expression> _assignVariableList = new List<Expression>();

		#endregion

		#endregion

		#region Main

		[STAThread]
		internal static void Main(string[] args)
		{
			try
			{
				#region Arguments

				if (args.Length != 2)
					throw new ArgumentException("arguments error!");

				var souceFilePath = args[0];
				var ruleFilePath = args[1];

				#endregion

				#region Create Col Expressions

				var rootElement = XElement.Load(ruleFilePath);

				var variableIndex = 1;
				foreach (var variableElement in rootElement.XPathSelectElements(XPATH_VARIABLE_ELEMENT))
					try
					{
						var typeElement = variableElement.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_TYPE);
						var nameElement = variableElement.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_VARIABLENAME);
						var assignElement = variableElement.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_ASSIGN);

						var variableEpression = Expression.Variable(_typeList[typeElement.Value.Trim()]);

						_variableList.Add(nameElement.Value.Trim(), variableEpression);
						_assignVariableList.Add(Expression.Assign(variableEpression, CreateExpression(assignElement)));

						variableIndex++;
					}
					catch (Exception ex)
					{
						throw new Exception("invalid variable:" + ex.Message + " #" + variableIndex.ToString());
					}

				#region Output

				var expList = new List<Func<string[], long, long, string>>();
				var outputIndex = 1;
				foreach (var colElement in rootElement.XPathSelectElements(XPATH_COL_ELEMENT))
					try
					{
						var expressionList = new List<Expression>(_assignVariableList);
						expressionList.Add(Expression.Call(CreateExpression(colElement), _toStringMethod));

						expList.Add(
							Expression.Lambda<Func<string[], long, long, string>>(
								Expression.Block(
									_variableList.Values,
									expressionList
								),
								_csv,
								_rowIndex,
								_colIndex
							).Compile());

						outputIndex++;
					}
					catch (Exception ex)
					{
						throw new Exception("invalid rule:" + ex.Message + " #" + outputIndex.ToString());
					}

				#endregion

				#endregion

				#region Create Filter Expression

				Func<string[], long, bool> filterMethod = null;

				try
				{
					var filterElement = rootElement.Elements().Where(childElement => childElement.Name == ELEMENT_NAME_FILTER);
					var filterElementCount = filterElement.Count();
					if (filterElementCount > 1)
						throw new Exception("duplicate filter");

					if (filterElementCount == 1)
					{
						var conditionExpression = Expression.Convert(CreateExpression(filterElement.FirstOrDefault()), _boolType);

						var expressionList = new List<Expression>(_assignVariableList);
						expressionList.Add(conditionExpression);

						filterMethod = Expression.Lambda<Func<string[], long, bool>>(Expression.Block(
										_variableList.Values,
										expressionList
									),
									_csv,
									_rowIndex
								).Compile();
					}
				}
				catch (Exception ex)
				{
					throw new Exception("invalid filter:" + ex.Message);
				}

				#endregion

				#region Do Expressions

				using (var reader = new CsvReader(souceFilePath))
				{
					reader.Delimiter = "\t";

					try
					{
						var fields = reader.ReadFields();
						while (!reader.EndOfData)
						{
							var lineNo = reader.LineNumber;

							if (filterMethod == null || filterMethod(fields, lineNo))
							{
								for (var col = 0; col < expList.Count; col++)
								{
									var colNo = col + 1;
									try
									{
										Console.Write("\"" + expList[col](fields, lineNo, colNo).Replace("\"", "\"\"") + "\",");
									}
									catch (Exception ex)
									{
										throw new Exception(ex.Message + string.Format(" #({0:d}, {1:d})", lineNo, (long)colNo));
									}
								}

								Console.Write(Environment.NewLine);
							}

							fields = reader.ReadFields();
						}
					}
					catch (Exception ex)
					{
						throw new Exception(ex.Message + string.Format(" line:#{0:d}", reader.LineNumber));
					}

					reader.Close();
				}

				#endregion
			}
			catch (Exception ex)
			{
				Console.WriteLine(Environment.NewLine + ex.Message);
			}

#if DEBUG
			Console.ReadKey();
#endif
		}

		#endregion

		#region Private Methods

		#region Create Expression

		private static Expression CreateExpression(XElement element)
		{
			var targetElement = element.GetTargetSingleElement(dummy => true);

			if(!_createExpressionDelegateList.ContainsKey(targetElement.Name))
				throw new Exception("illegal element is found");

			return _createExpressionDelegateList[targetElement.Name](targetElement);
		}

		#endregion

		#region Create Source Expression

		private static Expression CreateSourceExpression(XElement element)
		{
			#region init variables & check

			var sourceElement = element.GetTargetSingleElement(childElement => _sourceExpressionList.ContainsKey(childElement.Name));
			var sourceElementValue = sourceElement.Value.Trim();

			#endregion

			#region get source

			if(!_sourceExpressionList.ContainsKey(sourceElement.Name))
				throw new Exception("no source elements");

			return CreateCastParseExpression(element, _sourceExpressionList[sourceElement.Name](sourceElementValue));

			#endregion
		}

		#endregion

		#region Create Array Initialize Expression

		private static Expression CreateArrayInitExpression(XElement element)
		{
			var arrayItemElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_ARRAYITEMLIST).Elements(ELEMENT_NAME_ARRAYITEM);
			var typeElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_TYPE);
			var typeElementValue = typeElement.Value.Trim();
	
			if (!_typeList.ContainsKey(typeElementValue))
				throw new Exception("unknown type");

			return Expression.NewArrayInit(_typeList[typeElementValue], arrayItemElement.Select(childElement => Expression.Convert(CreateExpression(childElement), _typeList[typeElementValue])));
		}

		#endregion

		#region Create Calculate Expression

		private static Expression CreateCalculateExpression(XElement element)
		{
			#region init variables & check

			var methodElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_METHOD);
			var methodElementValue = methodElement.Value.Trim();
			var paramElementList = element.XPathSelectElements(XPATH_PARAM_ELEMENT);

			var expList = paramElementList.Select(param => CreateExpression(param)).ToArray();
	
			#endregion

			#region Expression

			Expression exp;
			
			if(_stringStaticMethodExpressionList.ContainsKey(methodElementValue))
				exp = _stringStaticMethodExpressionList[methodElementValue](expList);
			else
			{
				if(!_calculateExpressionTypeList.ContainsKey(methodElementValue))
					throw new Exception("unknown method");

				exp = Expression.MakeBinary(_calculateExpressionTypeList[methodElementValue], expList[0], expList[1]);
			}

			return CreateCastParseExpression(element, exp);

			#endregion
		}

		#endregion

		#region Create Conditional Expression

		private static Expression CreateConditionalExpression(XElement element)
		{
			var conditionElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_CONDITION);
			var matchElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_MATCH);
			var mismatchElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_MISMATCH);

			return CreateCastParseExpression(element, Expression.Condition(CreateExpression(conditionElement), CreateExpression(matchElement), CreateExpression(mismatchElement)));
		}

		#endregion

		#region Create Instance Expression

		private static Func<XElement, Expression> CreateInstanceExpression(Dictionary<string, Func<Expression, Expression[], Expression>> methodExpression, bool instanceToString)
		{
			return element =>
			{
				#region init variables & check

				var instanceElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_INSTANCE);
				var methodElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_METHOD);
				var methodElementValue = methodElement.Value.Trim();
				var paramElementList = element.XPathSelectElements(XPATH_PARAM_ELEMENT);
				if (paramElementList.Count() > 2)
					throw new Exception("invalid params");

				var expList = paramElementList.Select(param => CreateExpression(param)).ToArray();

				#endregion

				#region Expression

				if (!methodExpression.ContainsKey(methodElementValue))
					throw new Exception("invalid method");

				Expression instanceExpression;
				if(instanceToString)
					instanceExpression = Expression.Call(CreateExpression(instanceElement), _toStringMethod);
				else
					instanceExpression = CreateExpression(instanceElement);

				return CreateCastParseExpression(element, methodExpression[methodElementValue](instanceExpression, expList));

				#endregion
			};
		}

		#endregion

		#region Create Variable Expression

		private static Expression CreateVariableExpression(XElement element)
		{
			#region init variables & check

			var nameElement = element.GetTargetSingleElement(childElement => childElement.Name == ELEMENT_NAME_VARIABLENAME);
			var nameElementValue = nameElement.Value.Trim();

			#endregion

			#region get variable

			if (!_variableList.ContainsKey(nameElementValue))
				throw new Exception("undefined variable");

			return CreateCastParseExpression(element, _variableList[nameElementValue]);

			#endregion
		}

		#endregion

		#region Create Cast/Parse Expression

		private static Expression CreateCastParseExpression(XElement element, Expression exp)
		{
			var castElementList = element.Elements().Where(childNode => childNode.Name == ELEMENT_NAME_PARSE || childNode.Name == ELEMENT_NAME_CAST);
			if (castElementList.Count() > 2)
				throw new Exception("duplicate cast");

			var castElement = castElementList.FirstOrDefault();
			if (castElement == null)
				return exp;

			var castElementValue = castElement.Value.Trim();

			if (castElement.Name == ELEMENT_NAME_PARSE)
			{
				if (!_parseMethodList.ContainsKey(castElementValue))
					throw new Exception("invalid parse");

				return Expression.Call(_parseMethodList[castElementValue], exp);
			}
			else
			{
				if (castElementValue == TYPE_NAME_STRING)
					return Expression.Call(exp, _toStringMethod);

				if (!_typeList.ContainsKey(castElementValue))
					throw new Exception("invalid cast");

				return Expression.Convert(exp, _typeList[castElementValue]);
			}
		}

		#endregion

		#region Convert Array Items To String

		private static string[] ConvertToStringArray(Array targetArray)
		{
			var itemList = new List<string>();

			foreach (var item in targetArray)
				itemList.Add(item.ToString());

			return itemList.ToArray();
		}

		#endregion

		#region Set Array Item Value

		private static Array SetArrayItemValue(Array targetArray, object value, int index)
		{
			targetArray.SetValue(value, index);

			return targetArray;
		}

		#endregion

		#region Extension Method

		private static XElement GetTargetSingleElement(this XElement element, Func<XElement, bool> condition)
		{
			var targetElementList = element.Elements().Where(condition);
			if (targetElementList.Count() > 1)
				throw new Exception("duplicate elements");

			var targetElement = targetElementList.FirstOrDefault();
			if (targetElement == null)
				throw new Exception("no elements");

			return targetElement;
		}

		#endregion

		#endregion
	}

	#endregion
}

#endregion
