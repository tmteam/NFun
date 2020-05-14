using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.Errors;
using NFun.Tic.SolvingStates;
using NFun.Tic.Toposort;
using NFun.TypeInferenceCalculator;
using NFun.TypeInferenceCalculator.Errors;
using Array = NFun.Tic.SolvingStates.Array;

namespace NFun.Tic
{
    public class GraphBuilder
    {
        private readonly Dictionary<string, SolvingNode> _variables = new Dictionary<string, SolvingNode>();
        private readonly List<SolvingNode> _syntaxNodes = new List<SolvingNode>();
        private readonly List<SolvingNode> _typeVariables = new List<SolvingNode>();
        private int _varNodeId = 0;
        private readonly List<SolvingNode> _outputNodes = new List<SolvingNode>();
        private readonly List<SolvingNode> _inputNodes = new List<SolvingNode>();

        public RefTo InitializeVarNode(IType desc = null, Primitive anc = null, bool isComparable = false) 
            => new RefTo(CreateVarType(new Constrains(desc, anc){IsComparable =  isComparable}));
      

        #region set primitives

        public void SetVar(string name, int node)
        {
            var namedNode = GetNamedNode(name);
            var idNode = GetOrCreateNode(node);
            if (idNode.State is Constrains)
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
                SetOrCreatePrimitive(condId, Primitive.Bool);
        }


        public void SetConst(int id, Primitive type) 
            => SetOrCreatePrimitive(id, type);

        public void SetIntConst(int id, Primitive desc)
            => SetIntConst(id, desc, Primitive.Real, Primitive.Real);
        public bool TrySetIntConst(int id, Primitive desc)
            => TrySetIntConst(id, desc, Primitive.Real, Primitive.Real);
        public bool TrySetIntConst(int id, Primitive desc, Primitive anc, Primitive prefered)
        {
            var node = GetOrCreateNode(id);
            if (node.State is Constrains constrains)
            {
                constrains.AddAncestor(anc);
                constrains.AddDescedant(desc);
                constrains.Prefered = prefered;
                return true;
            }

            return false;
        }
        public void SetIntConst(int id, Primitive desc, Primitive anc, Primitive prefered)
        {
            if (!TrySetIntConst(id, desc,anc, prefered))
                throw new InvalidOperationException();
        }

        public void SetVarType(string s, IState state)
        {
            var node = GetNamedNode(s);

            if (state is Primitive primitive)
            {
                if (!node.TryBecomeConcrete(primitive))
                    throw new InvalidOperationException();
            }
            else if (state is ICompositeType composite)
            {
                RegistrateCompositeType(composite);
                node.State = state;
            }
            else
                throw new InvalidOperationException();
        }
        
        public void SetArrayConst(int id, Primitive elementType)
        {
            var eNode = CreateVarType(elementType);
            var node = GetOrCreateNode(id);
            if (node.State is Constrains c)
            {
                var arrayOf = Array.Of(eNode);
                if (c.Fits(arrayOf))
                {
                    node.State = arrayOf;
                    return;
                }
            }
            else if (node.State is Array a)
            {
                if(a.Element.Equals(elementType))
                    return;
            }
            throw new InvalidOperationException();
        }

        public void CreateLambda(int returnId, int lambdaId,params string[] varNames)
        {
            var args = varNames.Select(GetNamedNode).ToArray();
            var ret = GetOrCreateNode(returnId);
            SetOrCreateLambda(lambdaId, args,ret);
        }
        public void CreateLambda(int returnId, int lambdaId,IType returnType, params string[] varNames)
        {
            var args = varNames.Select(GetNamedNode).ToArray();
            var exprId = GetOrCreateNode(returnId);
            var returnTypeNode = CreateVarType(returnType);
            exprId.Ancestors.Add(returnTypeNode);
            //expr<=returnType<= ...
            SetOrCreateLambda(lambdaId, args, returnTypeNode);
        }

        public Fun SetFunDef(string name, int returnId, IType returnType = null, params string[] varNames)
        {
            var args = varNames.Select(GetNamedNode).ToArray();
            var exprId = GetOrCreateNode(returnId);
            var returnTypeNode = CreateVarType(returnType);
            //expr<=returnType<= ...
            exprId.Ancestors.Add(returnTypeNode);
            var fun = Fun.Of(args, returnTypeNode);

            var node = GetNamedNode(name);
            if(!(node.State is Constrains c) || !c.NoConstrains)
                throw new InvalidOperationException("variable "+ name+ "already declared");
            node.State = fun;
            _outputNodes.Add(returnTypeNode);
            _inputNodes.AddRange(args);
            return fun;

        }

        public RefTo SetStrictArrayInit(int resultIds, params int[] elementIds)
        {
            var elementType = CreateVarType();
            var resultNode = GetOrCreateArrayNode(resultIds, elementType);

            foreach (var id in elementIds)
            {
                elementType.BecomeReferenceFor(GetOrCreateNode(id));
                elementType.MemberOf.Add(resultNode);
            }
            return new RefTo(elementType);
        }
        public RefTo SetSoftArrayInit(int resultIds, params int[] elementIds)
        {
            var elementType = CreateVarType();
            var resultNode = GetOrCreateArrayNode(resultIds, elementType);

            foreach (var id in elementIds)
            {
                GetOrCreateNode(id).Ancestors.Add(elementType);
                elementType.MemberOf.Add(resultNode);
            }
            return new RefTo(elementType);
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
        public void SetCall(Fun fun, params int[] argThenReturnIds)
        {
            if (fun.ArgsCount != argThenReturnIds.Length - 1)
                throw new ArgumentException("Sizes of type and id array have to be equal");

            RegistrateCompositeType(fun);

            for (int i = 0; i < fun.ArgsCount; i++)
            {
                var state = fun.ArgNodes[i].State;
                if (state is Constrains)
                    state = new RefTo(fun.ArgNodes[i]);
                SetCallArgument(state, argThenReturnIds[i]);
            }

            var returnId = argThenReturnIds[argThenReturnIds.Length - 1];
            var returnNode = GetOrCreateNode(returnId);
            SolvingFunctions.Merge(fun.RetNode,returnNode);
        }

        /// <summary>
        /// Set function call, with function signature
        /// </summary>
        public void SetCall(IState[] argThenReturnTypes, int[] argThenReturnIds)
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

        private void SetCallArgument(IState type, int argId)
        {
            var node = GetOrCreateNode(argId);

            switch (type)
            {
                case Primitive primitive:
                {
                    if (!node.TrySetAncestor(primitive))
                        throw TicErrors.CannotSetState(node, primitive);
                    break;
                }
                case ICompositeType composite:
                {
                    RegistrateCompositeType(composite);

                    var ancestor = CreateVarType(composite);
                    node.Ancestors.Add(ancestor);
                    break;
                }
                case RefTo refTo:
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

            if (exprNode.State is Primitive primitive && defNode.State is Constrains constrains)
                    constrains.Prefered = primitive;

            exprNode.Ancestors.Add(defNode);
        }
        #endregion
        public SolvingNode[] Toposort()
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
                        if (TraceLog.IsEnabled)
                        {
                            TraceLog.WriteLine("Toposort results: ", ConsoleColor.Green);
                            TraceLog.WriteLine(string.Join("->", result.Order.Select(r => r.Name)));
                            TraceLog.WriteLine("Refs:" + string.Join(",", result.Refs.Select(r => r.Name)));

                        }
                        return result.Order.Union(result.Refs).ToArray();
                }
            }
        }

        public void PrintTrace()
        {
            if(!TraceLog.IsEnabled)
                return;
            
            var alreadyPrinted = new HashSet<SolvingNode>();

            var allNodes = _syntaxNodes.Union(_variables.Select(v => v.Value)).Union(_typeVariables);

            void ReqPrintNode(SolvingNode node)
            {
                if(node==null)
                    return;
                if(alreadyPrinted.Contains(node))
                    return;
                if(node.State is Array arr)
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

            if (TraceLog.IsEnabled)
            {
                TraceLog.WriteLine("Decycled:");
                PrintTrace();
                TraceLog.WriteLine();
                TraceLog.WriteLine("Set up");
            }

            SolvingFunctions.SetUpwardsLimits(sorted);
            if (TraceLog.IsEnabled)
            {
                PrintTrace();

                TraceLog.WriteLine();
                TraceLog.WriteLine("Set down");
            }

            SolvingFunctions.SetDownwardsLimits(sorted);
            if(TraceLog.IsEnabled)
                PrintTrace();

            DestructionFunctions.Destruction(sorted);

            if (TraceLog.IsEnabled)
            {
                TraceLog.WriteLine();
                TraceLog.WriteLine("Destruct Down");
                PrintTrace();
                TraceLog.WriteLine("Finalize");
            }

            var results = DestructionFunctions.FinalizeUp(sorted, 
                _outputNodes.ToArray(),
                _inputNodes.ToArray());

            if (TraceLog.IsEnabled)
            {

                TraceLog.WriteLine($"Type variables: {results.TypeVariables.Length}");
                foreach (var typeVariable in results.TypeVariables)
                    TraceLog.WriteLine("    " + typeVariable);

                TraceLog.WriteLine($"Syntax node types: ");
                foreach (var syntaxNode in results.SyntaxNodes.Where(s => s != null))
                    TraceLog.WriteLine("    " + syntaxNode);

                TraceLog.WriteLine($"Named node types: ");
                foreach (var namedNode in results.NamedNodes)
                    TraceLog.WriteLine("    " + namedNode);
            }

            return results;
        }
        private void SetCall(SolvingNode functionNode, int[] argThenReturnIds)
        {
            var id = argThenReturnIds[argThenReturnIds.Length - 1];

            var state = functionNode.State;
            if (state is RefTo r)
                state = r.Node.State;

            if (state is Fun fun)
            {
                if (fun.ArgsCount != argThenReturnIds.Length - 1)
                    throw TicErrors.InvalidFunctionalVarableSignature(functionNode);

                SetCall(fun, argThenReturnIds);

            }
            else if (state is Constrains constrains)
            {
                var idNode = GetOrCreateNode(id);

                var genericArgs = new SolvingNode[argThenReturnIds.Length - 1];
                for (int i = 0; i < argThenReturnIds.Length - 1; i++)
                    genericArgs[i] = CreateVarType();

                var newFunVar = Fun.Of(genericArgs, idNode);
                if (!constrains.Fits(newFunVar))
                    throw new InvalidOperationException("naaa");
                functionNode.State = newFunVar;

                SetCall(newFunVar, argThenReturnIds);
            }
            else
                throw new InvalidOperationException($"po po. functionNode.State is {functionNode.State}");
        }
        private SolvingNode GetNamedNode(string name)
        {
            if (_variables.TryGetValue(name, out var varnode))
            {
                return varnode;
            }

            var ans = new SolvingNode("T" + name, new Constrains(), SolvingNodeType.Named) { Registrated = true };
            _variables.Add(name, ans);
            return ans;
        }

        private void SetOrCreateLambda(int lambdaId, SolvingNode[] args,SolvingNode ret)
        {
            var fun = Fun.Of(args, ret);

            var alreadyExists = _syntaxNodes.GetOrEnlarge(lambdaId);
            if (alreadyExists != null)
            {
                alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(fun, alreadyExists.State)
                                      ?? throw TicErrors.CannotSetState(alreadyExists, fun);
            }
            else
            {
                var res = new SolvingNode(lambdaId.ToString(), fun, SolvingNodeType.SyntaxNode) {Registrated = true};
                _syntaxNodes[lambdaId] = res;
            }
        }

        private void SetOrCreatePrimitive(int id, Primitive type)
        {
            var node = GetOrCreateNode(id);
            if (!node.TryBecomeConcrete(type))
                throw TicErrors.CannotSetState(node, type);
        }

        private SolvingNode GetOrCreateArrayNode(int id, SolvingNode elementType)
        {
            var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
            if (alreadyExists != null)
            {
                alreadyExists.State = SolvingFunctions.GetMergedStateOrNull(new Array(elementType), alreadyExists.State)
                                      ?? throw TicErrors.CannotSetState(elementType, new Array(elementType));
                return alreadyExists;
            }

            var res = new SolvingNode(id.ToString(), new Array(elementType), SolvingNodeType.SyntaxNode) { Registrated = true};
            _syntaxNodes[id] = res;
            return res;
        }
        private SolvingNode GetOrCreateNode(int id)
        {
            var alreadyExists = _syntaxNodes.GetOrEnlarge(id);
            if (alreadyExists != null)
                return alreadyExists;

            var res = new SolvingNode(id.ToString(), new Constrains(), SolvingNodeType.SyntaxNode) {Registrated = true};
            _syntaxNodes[id] = res;
            return res;
        }
        private void RegistrateCompositeType(ICompositeType composite)
        {
            foreach (var member in composite.Members)
            {
                if (!member.Registrated)
                {
                    member.Registrated = true;
                    if (member.State is ICompositeType c)
                        RegistrateCompositeType(c);
                    _typeVariables.Add(member);

                }
            }
        }
        private SolvingNode CreateVarType(IState state = null)
        {
            if (state is ICompositeType composite)
                RegistrateCompositeType(composite);

            var varNode = new SolvingNode(
                    name: "V" + _varNodeId,
                    state: state ?? new Constrains(),
                    type: SolvingNodeType.TypeVariable)
                {Registrated = true};
            _varNodeId++;
            _typeVariables.Add(varNode);
            return varNode;
        }
    }
}
