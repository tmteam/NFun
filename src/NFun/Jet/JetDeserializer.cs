using System;
using System.Collections.Generic;
using System.Globalization;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.Runtime.Arrays;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Jet
{
    public class JetDeserializer
    {
        private readonly FunctionsDictionary _funDictionary;
        List<VarAttribute> _attributeBuffer = new List<VarAttribute>(2);
        List<Equation> _equations = new List<Equation>();
        VariableDictionary _variables = new VariableDictionary();
        List<VarInfo> _inputs = new List<VarInfo>();
        List<VarInfo> _outputs = new List<VarInfo>();

        private int _position = 0;
        private string[] _splitted;

        public JetDeserializer(string input, FunctionsDictionary funDictionary)
        {
            _funDictionary = funDictionary;
            _splitted = input.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static FunRuntime Deserialize(string jetString, FunctionsDictionary functions)
        {
            var deserializer = new JetDeserializer(jetString, functions);
            while (deserializer._position < deserializer._splitted.Length)
            {
                deserializer.ReadExpression();
            }
            return new FunRuntime(deserializer._equations, deserializer._variables, new List<UserFunction>());
        }

        private string ReadNext()
        {
            var res = _splitted[_position];
            _position++;
            return res;
        }
     
        private IExpressionNode ReadExpression()
        {
            var currentNodeId = ReadNext();
            switch (currentNodeId)
            {
                case JetSerializationHelper.ParameterlessAttributeId:
                    var attrName =  ReadNext();
                    _attributeBuffer.Add(new VarAttribute(attrName, null));
                    return ReadExpression();
                    


                case JetSerializationHelper.AttributeWithParameterId:
                    var attrWithValueName = ReadNext();
                    var attrWithValueValue = ReadNext();
                    _attributeBuffer.Add(new VarAttribute(attrWithValueName, attrWithValueValue));
                    return ReadExpression();



                case JetSerializationHelper.InputDefenitionId:
                    var inputName = ReadNext();
                    var inputType = JetSerializationHelper.ParseType(ReadNext());
                    _inputs.Add(new VarInfo(false, inputType, inputName, true, _attributeBuffer?.ToArray()));
                    _variables.TryAdd(new VariableSource(inputName, inputType, _attributeBuffer?.ToArray()));
                    _attributeBuffer?.Clear();
                    return null;



                case JetSerializationHelper.VariableId:
                    var varName = ReadNext();
                    return new VariableExpressionNode(_variables.GetSourceOrNull(varName), Interval.Empty);



                case JetSerializationHelper.ArrayId:
                    var arrayType = JetSerializationHelper.ParseType(ReadNext());
                    var sCount = ReadNext();
                    var count = int.Parse(sCount);
                    var items = new IExpressionNode[count];
                    for (int i = 0; i < count; i++)
                        items[i] = ReadExpression();
                    return new ArrayExpressionNode(items, Interval.Empty, VarType.ArrayOf(arrayType));
                


                case JetSerializationHelper.CastId:
                    var to = JetSerializationHelper.ParseType(ReadNext());
                    var node = ReadExpression();
                    return CastExpressionNode.GetConvertedOrOriginOrThrow(node, to);
                


                case JetSerializationHelper.ConstId:
                    var type  = JetSerializationHelper.ParseType(ReadNext());
                    var value = ParseConstantValue(type,ReadNext());
                    return new ConstantExpressionNode(value, type, Interval.Empty);



                case JetSerializationHelper.EquationId:
                    var outputName = ReadNext();
                    var expression = ReadExpression();
                    _outputs.Add(new VarInfo(true, expression.Type, outputName, true, _attributeBuffer?.ToArray()));
                    _variables.TryAdd(new VariableSource(outputName, expression.Type, _attributeBuffer?.ToArray()));
                    _equations.Add(new Equation(outputName, expression));
                    _attributeBuffer.Clear();
                    return null;



                case JetSerializationHelper.FunCallId:
                    var funId = ReadNext().Split(':');
                    var name = funId[0];
                    var returnType = JetSerializationHelper.ParseType(funId[1]);
                    var args = new IExpressionNode[funId.Length - 2];
                    var varTypes = new VarType[args.Length];
                    for (int i = 2; i < funId.Length; i++)
                    {
                        var argType = JetSerializationHelper.ParseType(funId[i]);
                        args[i-2] = ReadExpression();
                        varTypes[i - 2] = argType;
                    }
                    var function = _funDictionary.GetOrNullConcrete(name, returnType, varTypes)
                                   ?? throw new JetParseException("Function " + funId + " not found");
                    return new FunExpressionNode(function, args, Interval.Empty);



                case JetSerializationHelper.IfId:
                    var ifCount = int.Parse(ReadNext());
                    var ifExpressions = new IExpressionNode[ifCount];
                    var ifConditions = new IExpressionNode[ifCount];
                    for (int i = 0; i < ifCount; i++)
                    {
                        ifConditions[i] = ReadExpression();
                        ifExpressions[i] = ReadExpression();
                    }
                    var elseExpression = ReadExpression();
                    return new IfElseExpressionNode(ifExpressions, ifConditions, elseExpression, Interval.Empty, elseExpression.Type);
                
                
                
                case JetSerializationHelper.UserFunctionId:
                    var funDef = ReadNext().Split(':');
                    var userFunName = funDef[0];
                    var userFunReturnType = JetSerializationHelper.ParseType(funDef[1]);
                    var argsCount = (funDef.Length - 2) / 2;
                    var userFunArgs = new FunArgumentExpressionNode[argsCount];
                    for (int i = 0; i < argsCount; i++)
                    {
                        var argName = funDef[i * 2 + 2];
                        var argType = JetSerializationHelper.ParseType(funDef[i * 2 + 3]);
                        userFunArgs[i] = new FunArgumentExpressionNode(argName, argType, Interval.Empty);
                    }
                    throw new NotImplementedException();
                    //var userFunExpression = ReadExpression();
                    
                    //new UserFunction(userFunName, )
                    //_funDictionary.Add()
                    //return null;
                default:
                    throw new JetParseException($"Node of type {currentNodeId} is not supported");
            }
        }

        public static object ParseConstantValue(VarType type, string value)
        {
            switch (type.BaseType)
            {
                case BaseVarType.Bool:
                    return bool.Parse(value);
                case BaseVarType.UInt8:
                    return Byte.Parse(value);
                case BaseVarType.UInt16:
                    return UInt16.Parse(value);
                case BaseVarType.UInt32:
                    return UInt32.Parse(value);
                case BaseVarType.UInt64:
                    return UInt64.Parse(value);
                case BaseVarType.Int16:
                    return Int16.Parse(value);
                case BaseVarType.Int32:
                    return Int32.Parse(value);
                case BaseVarType.Int64:
                    return Int64.Parse(value);
                case BaseVarType.Real:
                    return Double.Parse(value, NumberFormatInfo.InvariantInfo);
                case BaseVarType.ArrayOf when  type.ArrayTypeSpecification.VarType== VarType.Char:
                    return new TextFunArray(JetSerializationHelper.FromJetEscaped(value));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
