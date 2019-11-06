using System;
using System.Collections.Generic;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.Interpritation.Nodes;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.Tokenization;
using NFun.Types;

namespace NFun.Jet
{
    public class JetDeserializer
    {
        private readonly FunctionsDictionary _funDictionary;
        readonly List<VarAttribute> _attributeBuffer = new List<VarAttribute>(2);
        readonly List<Equation> _equations = new List<Equation>();
        readonly List<VarInfo> _inputs = new List<VarInfo>();
        readonly List<VarInfo> _outputs = new List<VarInfo>();

        private readonly string[] _splitted;
        private int _position = 0;
        /// <summary>
        /// Current variables scope. 
        /// </summary>
        private VariableDictionary _currentVariables;
        
        public JetDeserializer(string input, FunctionsDictionary funDictionary)
        {
            //initialize body scope variables
            _currentVariables = new VariableDictionary();
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
            return new FunRuntime(deserializer._equations, deserializer._currentVariables, new List<UserFunction>());
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
                    _currentVariables.TryAdd(new VariableSource(inputName, inputType, _attributeBuffer?.ToArray()));
                    _attributeBuffer?.Clear();
                    return null;



                case JetSerializationHelper.VariableId:
                    //x {varName}
                    var varName = ReadNext();
                    return new VariableExpressionNode(_currentVariables.GetSourceOrNull(varName), Interval.Empty);



                case JetSerializationHelper.ArrayId:
                    //a {N} {item1} ... {itemN} 
                    var arrayType = JetSerializationHelper.ParseType(ReadNext());
                    var count = int.Parse(ReadNext());
                    var items = new IExpressionNode[count];
                    for (int i = 0; i < count; i++)
                        items[i] = ReadExpression();
                    return new ArrayExpressionNode(items, Interval.Empty, VarType.ArrayOf(arrayType));
                


                case JetSerializationHelper.CastId:
                    //c {toType} {expr}
                    var to = JetSerializationHelper.ParseType(ReadNext());
                    var node = ReadExpression();
                    return CastExpressionNode.GetConvertedOrOriginOrThrow(node, to);
                


                case JetSerializationHelper.ConstId:
                    //n {type} {value}
                    var type  = JetSerializationHelper.ParseType(ReadNext());
                    var value = JetSerializationHelper.ParseConstantValue(type,ReadNext());
                    return new ConstantExpressionNode(value, type, Interval.Empty);



                case JetSerializationHelper.EquationId:
                    var outputName = ReadNext();
                    var expression = ReadExpression();
                    _outputs.Add(new VarInfo(true, expression.Type, outputName, true, _attributeBuffer?.ToArray()));
                    _currentVariables.TryAdd(new VariableSource(outputName, expression.Type, _attributeBuffer?.ToArray()));
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



                case JetSerializationHelper.GenericCallId:
                    var genericFunId = ReadNext();
                    var genericReturnType = JetSerializationHelper.ParseType(ReadNext());
                    var genericArgsCount = Int32.Parse (ReadNext());
                    var genericArgTypes = new VarType[genericArgsCount];
                    for (int i = 0; i < genericArgsCount; i++)
                    {
                        genericArgTypes[i] = JetSerializationHelper.ParseType(ReadNext());
                    }
                    var genericFunction = _funDictionary.GetGenericOrNull(genericFunId, genericArgsCount)
                        ?? throw new JetParseException("Function " + genericFunId + " not found");
                    var concrete = genericFunction.CreateConcreteOrNull(genericReturnType, genericArgTypes);
                    var argExprs = new IExpressionNode[genericArgsCount];
                    for (int i = 0; i < genericArgsCount; i++)
                    {
                        argExprs[i] = ReadExpression();
                    }
                    return new FunExpressionNode(concrete, argExprs, Interval.Empty);



                case JetSerializationHelper.IfId:
                    //s {N} {conditionExpr1} {bodyExpr1} ... {conditionExprN} {bodyExprN} {elseExpr}
                    var ifCount = int.Parse(ReadNext());
                    var ifExpressions = new IExpressionNode[ifCount];
                    var ifConditions = new IExpressionNode[ifCount];
                    for (int i = 0; i < ifCount; i++)
                    {
                        ifConditions[i]  = ReadExpression();
                        ifExpressions[i] = ReadExpression();
                    }
                    var elseExpression = ReadExpression();
                    return new IfElseExpressionNode(ifExpressions, ifConditions, elseExpression, Interval.Empty, elseExpression.Type);
                
                
                
                case JetSerializationHelper.UserFunctionId:
                    var funDef = ReadNext().Split(':');
                    var userFunName = funDef[0];
                    var userFunReturnType = JetSerializationHelper.ParseType(funDef[1]);
                    var argsCount = (funDef.Length - 2) / 2;
                    var varSources = new VariableSource[argsCount];
                    var argTypes = new VarType[argsCount];

                    for (int i = 0; i < argsCount; i++)
                    {
                        var argName = funDef[i * 2 + 2];
                        var argType = JetSerializationHelper.ParseType(funDef[i * 2 + 3]);
                        varSources[i] = new VariableSource(argName, argType);
                        argTypes[i] = argType;
                    }
                    //use function prototype for recursive calls
                    var prototype = new ConcreteUserFunctionPrototype(userFunName, userFunReturnType, argTypes);
                    _funDictionary.Add(prototype);

                    //replace variable dictionary before read the expression
                    //because functions has different variable scope
                    var bodyVariableDictionary = _currentVariables;
                    _currentVariables = new VariableDictionary(varSources);
                    var userFunExpression = ReadExpression();
                    //restore bodyVariables
                    _currentVariables = bodyVariableDictionary;                    

                    prototype.SetActual(new UserFunction(
                        name: userFunName, 
                        variables: varSources, 
                        isReturnTypeStrictlyTyped: true, 
                        isGeneric: false,
                        expression: userFunExpression), 
                        Interval.Empty);
                    return null;



                case JetSerializationHelper.GenericUserFunctionId:
                    var genericFunDef = ReadNext().Split(':');
                    var genericUserFunName = genericFunDef[0];
                    var genericUserFunReturnType = JetSerializationHelper.ParseType(genericFunDef[1]);
                    var genericDefArgsCount = (genericFunDef.Length - 2) / 2;
                    var genericVarSources = new VariableSource[genericDefArgsCount];
                    var genericDefArgTypes = new VarType[genericDefArgsCount];

                    for (int i = 0; i < genericDefArgsCount; i++)
                    {
                        var argName = genericFunDef[i * 2 + 2];
                        var argType = JetSerializationHelper.ParseType(genericFunDef[i * 2 + 3]);
                        genericVarSources[i] = new VariableSource(argName, argType);
                        genericDefArgTypes[i] = argType;
                    }
                    //use function prototype for recursive calls
                    var genericPrototype = new GenericUserFunctionPrototype(genericUserFunName, genericUserFunReturnType, genericDefArgTypes);
                    _funDictionary.Add(genericPrototype);

                    //replace variable dictionary before read the expression
                    //because functions has different variable scope
                    var genericBodyVariableDictionary = _currentVariables;
                    _currentVariables = new VariableDictionary(genericVarSources);
                    var genericFunExpression = ReadExpression();
                    //restore bodyVariables
                    _currentVariables = genericBodyVariableDictionary;

                    genericPrototype.SetActual(new UserFunction(
                        name: genericUserFunName, 
                        variables: genericVarSources, 
                        isReturnTypeStrictlyTyped: true, 
                        isGeneric: true,  
                        expression: genericFunExpression), Interval.Empty);
                    return null;



                default:
                    throw new JetParseException($"Node of type {currentNodeId} is not supported");
            }
        }
    }
}
