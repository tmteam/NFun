using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.Tic.Toposort;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;

namespace NFun.Tic
{
    public class GraphBuilder
    {
        private readonly Dictionary<string, TicNode> _variables = new Dictionary<string, TicNode>();
        private readonly List<TicNode> _syntaxNodes;
        private readonly List<TicNode> _typeVariables = new List<TicNode>();
        private int _varNodeId = 0;
        private readonly List<TicNode> _outputNodes = new List<TicNode>();
        private readonly List<TicNode> _inputNodes = new List<TicNode>();

        public StateRefTo InitializeVarNode(ITypeState desc = null, StatePrimitive anc = null, bool isComparable = false) 
            => new StateRefTo(CreateVarType(new ConstrainsState(desc, anc){IsComparable =  isComparable}));

        //todo perfomance hotspot list capacity
        public GraphBuilder()
        {
            _syntaxNodes = new List<TicNode>(16);
        }
        
        #region set primitives

        public void SetVar(string name, int node)
        {
            var namedNode = GetNamedNode(name);
            var idNode = GetOrCreateNode(node);
            if (idNode.State is ConstrainsState)
            {
                namedNode.Ancestors.Add(idNode);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Node {node} cannot be referenced by '{name}' because it is not constrained node.");
            }
        }
        
        public void SetIfElse( int[] conditions, int[] expressions, int resultId)
        {
            var result = GetOrCreateNode(resultId);
            foreach (var exprId in expressions)
            {
                var expr = GetOrCreateNode(exprId);
                result.BecomeReferenceFor(expr);
            }

            foreach (var condId in conditions)
                SetOrCreatePrimitive(condId, StatePrimitive.Bool);
        }


        public void SetConst(int id, StatePrimitive type) 
            => SetOrCreatePrimitive(id, type);

        public void SetIntConst(int id, StatePrimitive desc)
            => SetIntConst(id, desc, StatePrimitive.Real, StatePrimitive.Real);
        public bool TrySetIntConst(int id, StatePrimitive desc)
            => TrySetIntConst(id, desc, StatePrimitive.Real, StatePrimitive.Real);
        public bool TrySetIntConst(int id, StatePrimitive desc, StatePrimitive anc, StatePrimitive prefered)
        {
            var node = GetOrCreateNode(id);
            if (node.State is ConstrainsState constrains)
            {
                constrains.AddAncestor(anc);
                constrains.AddDescedant(desc);
                constrains.Prefered = prefered;
                return true;
            }

            return false;
        }
        public void SetIntConst(int id, StatePrimitive desc, StatePrimitive anc, StatePrimitive prefered)
        {
            if (!TrySetIntConst(id, desc,anc, prefered))
                throw new InvalidOperationException();
        }

        public bool HasNamedNode(string s) => _variables.ContainsKey(s);

        public void SetVarType(string s, ITicNodeState state)
        {
            var node = GetNamedNode(s);

            if (state is StatePrimitive primitive)
            {
                if (!node.TryBecomeConcrete(primitive))
                    throw new InvalidOperationException();
            }
            else if (state is ICompositeTypeState composite)
            {
                RegistrateCompositeType(composite);
                node.State = state;
            }
            else
                throw new InvalidOperationException();
        }
        
        public void SetArrayConst(int id, StatePrimitive elementType)
        {
            var eNode = CreateVarType(elementType);
            var node = GetOrCreateNode(id);
            if (node.State is ConstrainsState c)
            {
                var arrayOf = StateArray.Of(eNode);
                if (c.Fits(arrayOf))
                {
                    node.State = arrayOf;
                    return;
                }
            }
            else if (node.State is StateArray a)
            {
                if(a.Element.Equals(elementType))
                    return;
            }
            throw new InvalidOperationException();
        }

        public void CreateLambda(int returnId, int lambdaId,params string[] varNames)
        {
            var args = GetNamedNodes(varNames);
            var ret  = GetOrCreateNode(returnId);
            SetOrCreateLambda(lambdaId, args,ret);
        }
        public void CreateLambda(int returnId, int lambdaId,ITypeState returnType, params string[] varNames)
        {
            var args   = GetNamedNodes(varNames);
            var exprId = GetOrCreateNode(returnId);
            var returnTypeNode = CreateVarType(returnType);
            exprId.Ancestors.Add(returnTypeNode);
            //expr<=returnType<= ...
            SetOrCreateLambda(lambdaId, args, returnTypeNode);
        }

        public StateFun SetFunDef(string name, int returnId, ITypeState returnType = null, params string[] varNames)
        {
            var args   = GetNamedNodes(varNames);
            var exprId = GetOrCreateNode(returnId);
            var returnTypeNode = CreateVarType(returnType);
            //expr<=returnType<= ...
            exprId.Ancestors.Add(returnTypeNode);
            var fun = StateFun.Of(args, returnTypeNode);

            var node = GetNamedNode(name);
            if(!(node.State is ConstrainsState c) || !c.NoConstrains)
                throw new InvalidOperationException("variable "+ name+ "already declared");
            node.State = fun;
            _outputNodes.Add(returnTypeNode);
            _inputNodes.AddRange(args);
            return fun;

        }

        public StateRefTo SetStrictArrayInit(int resultIds, params int[] elementIds)
        {
            var elementType = CreateVarType();
            var resultNode = GetOrCreateArrayNode(resultIds, elementType);

            foreach (var id in elementIds)
            {
                elementType.BecomeReferenceFor(GetOrCreateNode(id));
                elementType.IsMemberOfAnything = true;
            }
            return new StateRefTo(elementType);
        }
        public void SetSoftArrayInit(int resultIds, IEnumerable<int> elementIds)
        {
            var elementType = CreateVarType();
            GetOrCreateArrayNode(resultIds, elementType);
            foreach (var id in elementIds)
            {
                GetOrCreateNode(id).Ancestors.Add(elementType);
                elementType.IsMemberOfAnything = true;
            }
        }
        /// <summary>
        /// Set function call, where function variable (or expression) placed at bodyId
        /// </summary>
        public void SetCall(int bodyId, params int[] argThenReturnIds)
        {
            var node = GetOrCreateNode(bodyId);
            SetCall(node, argThenReturnIds);
        }
        /// <summary>
        /// Set function call, of function variable with id of name
        /// </summary>
        public void SetCall(string name, params int[] argThenReturnIds)
        {
            var namedNode =GetNamedNode(name);
            SetCall(namedNode, argThenReturnIds);
        }


        /// <summary>
        /// Set function call, of already known functional type 
        /// </summary>
        public void SetCall(StateFun funState, params int[] argThenReturnIds)
        {
            if (funState.ArgsCount != argThenReturnIds.Length - 1)
                throw new ArgumentException("Sizes of type and id array have to be equal");

            RegistrateCompositeType(funState);

            for (int i = 0; i < funState.ArgsCount; i++)
            {
                var state = funState.ArgNodes[i].State;
                if (state is ConstrainsState)
                    state = new StateRefTo(funState.ArgNodes[i]);
                SetCallArgument(state, argThenReturnIds[i]);
            }

            var returnId = argThenReturnIds[argThenReturnIds.Length - 1];
            var returnNode = GetOrCreateNode(returnId);
            SolvingFunctions.Merge(funState.RetNode,returnNode);
        }

        /// <summary>
        /// Set function call, with function signature
        /// </summary>
        public void SetCall(ITicNodeState[] argThenReturnTypes, int[] argThenReturnIds)
        {
            if(argThenReturnTypes.Length!=argThenReturnIds.Length)
                throw new ArgumentException("Sizes of type and id array have to be equal");

            for (int i = 0; i < argThenReturnIds.Length - 1; i++)
                SetCallArgument(argThenReturnTypes[i], argThenReturnIds[i]);

            var returnType = argThenReturnTypes[argThenReturnIds.Length - 1];

            var returnId = argThenReturnIds[argThenReturnIds.Length - 1];
            var returnNode = GetOrCreateNode(returnId);
            returnNode.State = SolvingFunctions.GetMergedStateOrNull(returnNode.State, returnType)
                               ?? throw TicErrors.CannotSetState(returnNode, returnType);
        }

        private void SetCallArgument(ITicNodeState type, int argId)
        {
            var node = GetOrCreateNode(argId);

            switch (type)
            {
                case StatePrimitive primitive:
                {
                    if (!node.TrySetAncestor(primitive))
                        throw TicErrors.CannotSetState(node, primitive);
                    break;
                }
                case ICompositeTypeState composite:
                {
                    RegistrateCompositeType(composite);

                    var ancestor = CreateVarType(composite);
                    node.Ancestors.Add(ancestor);
                    break;
                }
                case StateRefTo refTo:
                {
                    node.Ancestors.Add(refTo.Node);
                    break;
                }

                default: throw new NotSupportedException();
            }
        }

        public void SetDef(string name, int rightNodeId)
        {
            var exprNode = GetOrCreateNode(rightNodeId);
            var defNode = GetNamedNode(name);
            _outputNodes.Add(defNode);

            if (exprNode.State is StatePrimitive primitive && defNode.State is ConstrainsState constrains)
                    constrains.Prefered = primitive;

            exprNode.Ancestors.Add(defNode);
        }
        #endregion
        
        //todo perfomance hotspot
        public TicNode[] Toposort()
        {
            int iteration = 0;
            while (true)
            {

                var allNodes = _syntaxNodes
                    .Where(s=>s!=null)
                    .Concat(_variables.Values)
                    .Concat(_typeVariables)
                    .ToArray();
                
                if (iteration > allNodes.Length * allNodes.Length)
                    throw new InvalidOperationException("Infinite cycle detected. Types cannot be solved");
                iteration++;

                var result = NodeToposortFunctions.Toposort(allNodes);

                switch (result.Status)
                {
                    case SortStatus.MemebershipCycle: throw TicErrors.RecursiveTypeDefenition(result.Order.ToArray());
                    case SortStatus.AncestorCycle:
                    {
                        var cycle = result.Order;
                        TraceLog.WriteLine("Found cycle: ", ConsoleColor.Yellow);
                        TraceLog.WriteLine(()=>string.Join("->", cycle.Select(r => r.Name)));

                        //main node. every other node has to reference on it
                        SolvingFunctions.MergeGroup(cycle);

                        if (TraceLog.IsEnabled)
                        {
                            TraceLog.WriteLine($"Cycle normalization results: ", ConsoleColor.Green);
                            foreach (var solvingNode in cycle)
                                solvingNode.PrintToConsole();
                        }

                        break;
                    }

                    case SortStatus.Sorted:
#if DEBUG
                            TraceLog.WriteLine("Toposort results: ", ConsoleColor.Green);
                            TraceLog.WriteLine(string.Join("->", result.Order.Select(r => r.Name)));
                            TraceLog.WriteLine("Refs:" + string.Join(",", result.Refs.Select(r => r.Name)));
#endif
                    return result.Order.Union(result.Refs).ToArray();
                }
            }
        }

        public void PrintTrace()
        {
            if(!TraceLog.IsEnabled)
                return;
            
            var alreadyPrinted = new HashSet<TicNode>();

            var allNodes = _syntaxNodes.Union(_variables.Select(v => v.Value)).Union(_typeVariables);

            void ReqPrintNode(TicNode node)
            {
                if(node==null)
                    return;
                if(alreadyPrinted.Contains(node))
                    return;
                if(node.State is StateArray arr)
                    ReqPrintNode(arr.ElementNode);
                node.PrintToConsole();
                alreadyPrinted.Add(node);
            }

            foreach (var node in allNodes)
                ReqPrintNode(node);
        }
       
        public FinalizationResults Solve()
        {
            if (TraceLog.IsEnabled) {
                PrintTrace();
                TraceLog.WriteLine();
            }

            var sorted = Toposort();
#if DEBUG
            if (TraceLog.IsEnabled)
            {
                TraceLog.WriteLine("Decycled:");
                PrintTrace();
                TraceLog.WriteLine();
                TraceLog.WriteLine("Set up");
            }
#endif

            SolvingFunctions.SetUpwardsLimits(sorted);
#if DEBUG
            if (TraceLog.IsEnabled)
            {
                PrintTrace();

                TraceLog.WriteLine();
                TraceLog.WriteLine("Set down");
            }
            #endif


            SolvingFunctions.SetDownwardsLimits(sorted);
#if DEBUG

            if(TraceLog.IsEnabled)
                PrintTrace();
#endif
            DestructionFunctions.Destruction(sorted);

#if DEBUG
            if (TraceLog.IsEnabled)
            {
                TraceLog.WriteLine();
                TraceLog.WriteLine("Destruct Down");
                PrintTrace();
                TraceLog.WriteLine("Finalize");
            }
#endif

            var results = DestructionFunctions.FinalizeUp(sorted, _outputNodes, _inputNodes);
#if DEBUG
            if (TraceLog.IsEnabled)
            {

                TraceLog.WriteLine($"Type variables: {results.TypeVariables.Count()}");
                foreach (var typeVariable in results.TypeVariables)
                    TraceLog.WriteLine("    " + typeVariable);

                TraceLog.WriteLine($"Syntax node types: ");
                foreach (var syntaxNode in results.SyntaxNodes.Where(s => s != null))
                    TraceLog.WriteLine("    " + syntaxNode);

                TraceLog.WriteLine($"Named node types: ");
                foreach (var namedNode in results.NamedNodes)
                    TraceLog.WriteLine("    " + namedNode);
            }
#endif
            return results;
        }
        private void SetCall(TicNode functionNode, int[] argThenReturnIds)
        {
            var id = argThenReturnIds[argThenReturnIds.Length - 1];

            var state = functionNode.State;
            if (state is StateRefTo r)
                state = r.Node.State;

            if (state is StateFun fun)
            {
                if (fun.ArgsCount != argThenReturnIds.Length - 1)
                    throw TicErrors.InvalidFunctionalVarableSignature(functionNode);

                SetCall(fun, argThenReturnIds);

            }
            else if (state is ConstrainsState constrains)
            {
                var idNode = GetOrCreateNode(id);

                var genericArgs = new TicNode[argThenReturnIds.Length - 1];
                for (int i = 0; i < argThenReturnIds.Length - 1; i++)
                    genericArgs[i] = CreateVarType();

                var newFunVar = StateFun.Of(genericArgs, idNode);
                if (!constrains.Fits(newFunVar))
                    throw new InvalidOperationException("naaa");
                functionNode.State = newFunVar;

                SetCall(newFunVar, argThenReturnIds);
            }
            else
                throw new InvalidOperationException($"po po. functionNode.State is {functionNode.State}");
        }

        private TicNode[] GetNamedNodes(string[] names)
        {
            var ans = new TicNode[names.Length];
            for (int i = 0; i < names.Length; i++)
                ans[i] = GetNamedNode(names[i]);

            return ans;
        }
            private TicNode GetNamedNode(string name)
        {
            if (_variables.TryGetValue(name, out var varnode))
            {
                return varnode;
            }

            var ans = new TicNode("T" + name, new ConstrainsState(), TicNodeType.Named) { Registrated = true };
            _variables.Add(name, ans);
            return ans;
        }

        private void SetOrCreateLambda(int lambdaId, TicNode[] args,TicNode ret)
        {
            var fun = StateFun.Of(args, ret);

            var alreadyExists = _syntaxNodes.GetOrEnlarge(lambdaId);
            if (alreadyExists != null)
            {
                alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(fun, alreadyExists.State)
                                      ?? throw TicErrors.CannotSetState(alreadyExists, fun);
            }
            else
            {
                var res = new TicNode(lambdaId.ToString(), fun, TicNodeType.SyntaxNode) {Registrated = true};
                _syntaxNodes[lambdaId] = res;
            }
        }

        private void SetOrCreatePrimitive(int id, StatePrimitive type)
        {
            var node = GetOrCreateNode(id);
            if (!node.TryBecomeConcrete(type))
                throw TicErrors.CannotSetState(node, type);
        }

        private TicNode GetOrCreateArrayNode(int id, TicNode elementType)
        {
            var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
            if (alreadyExists != null)
            {
                alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(new StateArray(elementType), alreadyExists.State)
                                      ?? throw TicErrors.CannotSetState(elementType, new StateArray(elementType));
                return alreadyExists;
            }

            var res = new TicNode(id.ToString(), new StateArray(elementType), TicNodeType.SyntaxNode) { Registrated = true};
            _syntaxNodes[id] = res;
            return res;
        }
        private TicNode GetOrCreateNode(int id)
        {
            var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
            if (alreadyExists != null)
                return alreadyExists;

            var res = new TicNode(id.ToString(), new ConstrainsState(), TicNodeType.SyntaxNode) {Registrated = true};
            _syntaxNodes[id] = res;
            return res;
        }
        
        private void RegistrateCompositeType(ICompositeTypeState composite)
        {
            foreach (var member in composite.Members)
            {
                if (!member.Registrated)
                {
                    member.Registrated = true;
                    if (member.State is ICompositeTypeState c)
                        RegistrateCompositeType(c);
                    _typeVariables.Add(member);

                }
            }
        }
        private TicNode CreateVarType(ITicNodeState state = null)
        {
            if (state is ICompositeTypeState composite)
                RegistrateCompositeType(composite);

            var varNode = new TicNode(
                    name: "V" + _varNodeId,
                    state: state ?? new ConstrainsState(),
                    type: TicNodeType.TypeVariable)
                {Registrated = true};
            _varNodeId++;
            _typeVariables.Add(varNode);
            return varNode;
        }
    }
}
