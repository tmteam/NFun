using System;
using System.Collections.Generic;
using System.Linq;
using NFun.BuiltInFunctions;
using NFun.HindleyMilner;
using NFun.HindleyMilner.Tyso;
using NFun.Interpritation;
using NFun.Interpritation.Functions;
using NFun.ParseErrors;
using NFun.Parsing;
using NFun.Runtime;
using NFun.SyntaxParsing;
using NFun.SyntaxParsing.Visitors;
using NFun.Tokenization;
using NFun.Types;

namespace NFun
{
    public  class FunBuilder
    {
        private readonly string _text;

        public static FunBuilder With(string text) => new FunBuilder(text);

        private FunBuilder(string text)
        {
            _text = text;
        }

        readonly List<FunctionBase> _functions = new List<FunctionBase>();
        readonly List<GenericFunctionBase> _genericFunctions= new List<GenericFunctionBase>();
        public FunBuilder WithFunctions(params FunctionBase[] functions)
        {
            _functions.AddRange(functions);
            return this;
        }
        public FunBuilder WithFunctions(params GenericFunctionBase[] functions)
        {
            _genericFunctions.AddRange(functions);
            return this;
        }

        public FunRuntime Build()
        {
            var flow = Tokenizer.ToFlow(_text);
            var syntaxTree = TopLevelParser.Parse(flow);

            //Set node numbers
            syntaxTree.ComeOver(new SetNodeNumberVisitor());
            
            //get topology sort of the functions
            var functionSolveOrder  = FindFunctionsSolvingOrderOrThrow(syntaxTree);
            var functionsDictionary = MakeFunctionsDictionary();

            foreach (var functionSyntaxNode in functionSolveOrder)
            {
                //make alias for function name
                var funAlias = functionSyntaxNode.Id+"("+functionSyntaxNode.Args.Count+")";

                //introduce function variable here
                var visitorInitState = CreateVisitorStateFor(functionSyntaxNode, funAlias);
                
                //solving each function
                 var typeSolving = new HmAlgorithmAdapter(functionsDictionary,visitorInitState);
                
                 visitorInitState.CurrentSolver.SetFunDefenition(funAlias, functionSyntaxNode.NodeNumber, functionSyntaxNode.Body.NodeNumber);
                 // solve the types
                 var types = typeSolving.Apply(functionSyntaxNode);
                 if(!types.IsSolved)
                    throw new FunParseException(-4, $"Function '{functionSyntaxNode.Id}' is not solved", 0,0);
                 
                 //set types to nodes
                 functionSyntaxNode.ComeOver(new ApplyHmResultVisitor(types));
                 
                 var funType = types.GetVarType(funAlias);
                 //make function prototype
                 var prototype = new FunctionPrototype(functionSyntaxNode.Id, 
                     funType.FunTypeSpecification.Output, 
                     funType.FunTypeSpecification.Inputs);
                 //add prototype to dictionary for future use
                 functionsDictionary.Add(prototype);
            }
            
            var bodyTypeSolving = new HmAlgorithmAdapter(functionsDictionary).Apply(syntaxTree);
            if (!bodyTypeSolving.IsSolved)
                throw new InvalidOperationException("Types not solved");
            foreach (var syntaxNode in syntaxTree.Children)
            {
                //function nodes were solved above
                if(syntaxNode is UserFunctionDefenitionSyntaxNode)
                    continue;
                
                //set types to nodes
                syntaxNode.ComeOver(new ApplyHmResultVisitor(bodyTypeSolving));
            }
            return ExpressionReader.Interpritate(
                syntaxTree, 
                _functions.Concat(PredefinedFunctions), 
                _genericFunctions.Concat(predefinedGenerics));
        }

        public HmVisitorState CreateVisitorStateFor(UserFunctionDefenitionSyntaxNode node, string funAlias)
        {
            var visitorState = new HmVisitorState(new NsHumanizerSolver());
            
            //Add user function as a functional variable

            //make outputType
            var outputType =  visitorState.CreateTypeNode(node.SpecifiedType);
            
            //create input variables
            var argTypes = new List<SolvingNode>();
            foreach (var argNode in node.Args)
            {
                //make aliases for input variables
                var inputAlias = funAlias + "::" + argNode.Id;
                visitorState.AddVariableAliase(argNode.Id, inputAlias);
                
                if (argNode.VarType.BaseType == BaseVarType.Empty)
                {
                    //variable type is not specified
                    var genericVarType = visitorState.CurrentSolver.SetNewVar(inputAlias);
                    argTypes.Add(genericVarType);
                }
                else
                {
                    //variable type is specified
                    var hmType = argNode.VarType.ConvertToHmType();
                    visitorState.CurrentSolver.SetVarType(inputAlias, hmType);
                    argTypes.Add(SolvingNode.CreateStrict(hmType));
                }
                    
            }
            //set function variable defenition
            visitorState.CurrentSolver
                .SetVarType(funAlias, FType.Fun(outputType, argTypes.ToArray()));
            return visitorState;
        }
        
        /// <summary>
        /// Gets order of calculating the functions, based on its co using.
        /// </summary>
        private static UserFunctionDefenitionSyntaxNode[] FindFunctionsSolvingOrderOrThrow(SyntaxTree syntaxTree)
        {
            var userFunctions = syntaxTree.Children.OfType<UserFunctionDefenitionSyntaxNode>().ToList();

            var userFunctionsNames = new Dictionary<string, int>();
            int i = 0;
            foreach (var userFunction in userFunctions)
            {
                userFunctionsNames.Add(userFunction.Id + "(" + userFunction.Args.Count + ")", i);
                i++;
            }

            int[][] dependenciesGraph = new int[i][];
            int j = 0;
            foreach (var userFunction in userFunctions)
            {
                var visitor = new FindFunctionDependenciesVisitor(userFunctionsNames);
                if (!userFunction.ComeOver(visitor))
                    throw new InvalidOperationException("User fun come over");
                dependenciesGraph[j] = visitor.GetFoundDependencies();
                j++;
            }

            var sortResults = GraphTools.SortCycledTopology(dependenciesGraph);
            if (sortResults.HasCycle)
                throw new InvalidOperationException("Cycled functions found");

            var functionSolveOrder = new UserFunctionDefenitionSyntaxNode[sortResults.NodeNames.Length];
            for (int k = 0; k < sortResults.NodeNames.Length; k++)
                functionSolveOrder[k] = userFunctions[sortResults.NodeNames[k]];
            return functionSolveOrder;
        }

        private FunctionsDictionary MakeFunctionsDictionary()
        {
            var functionsDictionary = new FunctionsDictionary();
            foreach (var predefinedFunction in _functions.Concat(PredefinedFunctions))
                functionsDictionary.Add(predefinedFunction);
            foreach (var genericFunctionBase in _genericFunctions.Concat(predefinedGenerics))
                functionsDictionary.Add(genericFunctionBase);
            return functionsDictionary;
        }

        public static IEnumerable<FunctionBase> PredefinedFunctions => _predefinedFunctions;
        public static IEnumerable<GenericFunctionBase> PredefinedGenericFunctions => predefinedGenerics;

        internal static readonly GenericFunctionBase[] predefinedGenerics =
        {
            new IsInSingleGenericFunctionDefenition(), 
            new IsInMultipleGenericFunctionDefenition(), 
            new ReiterateGenericFunctionDefenition(),
            new UniqueGenericFunctionDefenition(), 
            new UniteGenericFunctionDefenition(), 
            new IntersectGenericFunctionDefenition(), 
            new SubstractArraysGenericFunctionDefenition(), 
            
           // new ConcatArraysGenericFunctionDefenition(CoreFunNames.ArrConcat), 
            new ConcatArraysGenericFunctionDefenition("concat"), 

            new SetGenericFunctionDefenition(),
            new GetGenericFunctionDefenition(),
            new SliceGenericFunctionDefenition(), 
            new SliceWithStepGenericFunctionDefenition(), 
            new FindGenericFunctionDefenition(), 
            new ReduceWithDefaultsGenericFunctionDefenition(),
            new ReduceGenericFunctionDefenition(),
            new TakeGenericFunctionDefenition(),
            new SkipGenericFunctionDefenition(),
            new RepeatGenericFunctionDefenition(),
            new FilterGenericFunctionDefenition(),
            new FlatGenericFunctionDefenition(),
            new ChunkGenericFunctionDefenition(),
            new MapGenericFunctionDefenition(),
            new AllGenericFunctionDefenition(), 
            new AnyGenericFunctionDefenition(), 
            new ReverseGenericFunctionDefenition(),
        };
        internal static readonly FunctionBase[] _predefinedFunctions = 
            {
                new InvertFunction(), 
                new AndFunction(), 
                new OrFunction(), 
                new XorFunction(), 
                new EqualFunction(), 
                new NotEqualFunction(), 
                new LessIntFunction(), 
                new LessRealFunction(), 
                new LessOrEqualIntFunction(), 
                new LessOrEqualRealFunction(), 
                new MoreIntFunction(), 
                new MoreRealFunction(), 
                new MoreOrEqualIntFunction(), 
                new MoreOrEqualRealFunction(), 
                new BitShiftLeftFunction(), 
                new BitShiftRightFunction(), 
                new AbsOfRealFunction(),
                new AbsOfIntFunction(),
                
                new AddRealFunction(CoreFunNames.Add),
                new AddIntFunction(CoreFunNames.Add),
                new AddTextFunction(CoreFunNames.Add),
                
                new AddRealFunction("sum"),
                new AddIntFunction("sum"),
                new AddInt64Function("sum"), 
                new AddTextFunction("str_concat"),

                new SubstractIntFunction(), 
                new SubstractRealFunction(), 
                new BitAndIntFunction(),
                new BitOrIntFunction(),
                new BitXorIntFunction(),
                new BitInverseIntFunction(), 
                new PowRealFunction(), 
                new MultiplyIntFunction(), 
                new MultiplyRealFunction(), 
                new DivideRealFunction(), 
                new RemainderRealFunction(), 
                new RemainderIntFunction(), 
                    
                new SinFunction(), 
                new CosFunction(),
                new TanFunction(),
                new AtanFunction(),
                new Atan2Function(),
                new AsinFunction(), 
                new AcosFunction(), 
                new ExpFunction(), 
                new LogFunction(), 
                new LogEFunction(), 
                new Log10Function(), 
                new FloorFunction(), 
                new CeilFunction(), 
                new RoundToIntFunction(), 
                new RoundToRealFunction(), 
                new SignFunction(),
                new ToTextFunction(), 
                new ToIntFromRealFunction(), 
                new ToIntFromTextFunction(), 
                new ToIntFromBytesFunction(),
                new ToRealFromIntFunction(), 
                new ToRealFromTextFunction(), 
                new ToUtf8Function(), 
                new ToUnicodeFunction(), 
                new ToBytesFromIntFunction(), 
                new ToBitsFromIntFunction(), 

                new EFunction(), 
                new PiFunction(),
                new CountFunction(),
                new AverageFunction(),
                new MaxOfIntFunction(), 
                new MaxOfInt64Function(),
                new MaxOfRealFunction(), 
                new MinOfIntFunction(), 
                new MinOfInt64Function(),
                new MinOfRealFunction(), 
                new MultiMaxIntFunction(), 
                new MultiMaxRealFunction(),
                new MultiMinIntFunction(), 
                new MultiMinRealFunction(),
                new MultiSumIntFunction(), 
                new MultiSumRealFunction(), 
                new MedianIntFunction(), 
                new MedianRealFunction(),
                new AnyFunction(), 
                new SortIntFunction(), 
                new SortRealFunction(), 
                new SortTextFunction(), 
                new RangeIntFunction(),
                new RangeWithStepIntFunction(),
                new RangeWithStepRealFunction(),
            };
        public static FunRuntime BuildDefault(string text)
            => FunBuilder.With(text).Build();
    }
}