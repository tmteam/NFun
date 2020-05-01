using System;
using System.Collections.Generic;
using System.Linq;
using NFun.Tic.SolvingStates;
using NFun.Tic.Toposort;
using NFun.TypeInferenceCalculator;
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
        {
            var node = GetOrCreateNode(id);
            if (node.State is Constrains constrains)
            {
                constrains.AddAncestor(Primitive.Real);
                constrains.AddDescedant(desc);
                constrains.Prefered = Primitive.Real;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        public void SetVarType(string s, IState state)
        {
            var node = GetNamedNode(s);

           if (state is Primitive primitive)
            {
                if (!node.TryBecomeConcrete(primitive))
                    throw new InvalidOperationException();
            }
            else if (state is Array _)
            {
                node.State = state;
            }
            else if (state is Fun _)
            {
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
                if(a.Element== elementType)
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

        public void SetArrayInit(int resultIds, params int[] elementIds)
        {
            var elementType = CreateVarType();
            var resultNode = GetOrCreateArrayNode(resultIds, elementType);

            foreach (var id in elementIds)
            {
                elementType.BecomeReferenceFor(GetOrCreateNode(id));
                elementType.MemberOf.Add(resultNode);
            }
        }

        public void SetCall(IState[] argThenReturnTypes, int[] argThenReturnIds)
        {
            if(argThenReturnTypes.Length!=argThenReturnIds.Length)
                throw new ArgumentException("Sizes of type and id array have to be equal");

            for (int i = 0; i < argThenReturnIds.Length - 1; i++)
            {
                var type = argThenReturnTypes[i];
                var argId = argThenReturnIds[i];
                switch (type)
                {
                    case Primitive primitive:
                    {
                        var node = GetOrCreateNode(argId);
                        if(!node.TrySetAncestor(primitive))
                            throw new InvalidOperationException();
                        break;
                    }
                    case Array array:
                    {
                        //var node = GetOrCreateNode(argId);
                        //var ancestor = CreateVarType(array);
                        //ancestor.BecomeAncestorFor(node);

                        //todo Upcast support
                        GetOrCreateArrayNode(argId, array.ElementNode);
                        break;
                    }
                    case Fun fun:
                    {
                        var node = GetOrCreateNode(argId);
                        var ancestor = CreateVarType(fun);
                        ancestor.BecomeAncestorFor(node);
                        break;
                    }
                    case RefTo refTo:
                    {
                        var node = GetOrCreateNode(argId);
                        refTo.Node.BecomeAncestorFor(node);
                        break;
                    }
                    default: throw new InvalidOperationException();
                }
            }

            var returnId = argThenReturnIds[argThenReturnIds.Length - 1];
            var returnType = argThenReturnTypes[argThenReturnIds.Length - 1];
            var returnNode = GetOrCreateNode(returnId);
            returnNode.State =  SolvingFunctions.GetMergedState(returnNode.State, returnType);
        }
        public void SetDef(string name, int rightNodeId)
        {
            var exprNode = GetOrCreateNode(rightNodeId);
            var defNode = GetNamedNode(name);
            _outputNodes.Add(defNode);

            if (exprNode.State is Primitive primitive && defNode.State is Constrains constrains)
                    constrains.Prefered = primitive;
            defNode.BecomeAncestorFor(exprNode);
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
                    case SortStatus.MemebershipCycle: throw new InvalidOperationException("Reqursive type defenition");
                    case SortStatus.AncestorCycle:
                    {
                        var cycle = result.Order;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Found cycle: ");
                        Console.ResetColor();
                        Console.WriteLine(string.Join("->", cycle.Select(r => r.Name)));

                        //main node. every other node has to reference on it
                        SolvingFunctions.MergeGroup(cycle);

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Cycle normalization results: ");
                        Console.ResetColor();
                        foreach (var solvingNode in cycle)
                            solvingNode.PrintToConsole();
                        break;
                    }

                    case SortStatus.Sorted:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Toposort results: ");
                        Console.ResetColor();
                        Console.WriteLine(string.Join("->", result.Order.Select(r => r.Name)));
                        Console.WriteLine("Refs:" + string.Join(",", result.Refs.Select(r => r.Name)));

                        return result.Order.Union(result.Refs).ToArray();
                }
            }
        }

        public void PrintTrace()
        {
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
            PrintTrace();
            Console.WriteLine();

            var sorted = Toposort();

            Console.WriteLine("Decycled:");
            PrintTrace();

            Console.WriteLine();
            Console.WriteLine("Set up");

            SolvingFunctions.SetUpwardsLimits(sorted);
            PrintTrace();

            Console.WriteLine();
            Console.WriteLine("Set down");

            SolvingFunctions.SetDownwardsLimits(sorted);
            PrintTrace();

            SolvingFunctions.Destruction(sorted);

            Console.WriteLine();
            Console.WriteLine("Destruct Down");
            PrintTrace();

            Console.WriteLine("Finalize");
            var results = SolvingFunctions.FinalizeUp(sorted, _outputNodes.ToArray());

            Console.WriteLine($"Type variables: {results.TypeVariables.Length}");
            foreach (var typeVariable in results.TypeVariables)
                Console.WriteLine("    " + typeVariable);

            Console.WriteLine($"Syntax node types: ");
            foreach (var syntaxNode in results.SyntaxNodes.Where(s => s != null))
                Console.WriteLine("    " + syntaxNode);

            Console.WriteLine($"Named node types: ");
            foreach (var namedNode in results.NamedNodes)
                Console.WriteLine("    " + namedNode);

            return results;
        }

        private SolvingNode GetNamedNode(string name)
        {
            if (_variables.TryGetValue(name, out var varnode))
            {
                return varnode;
            }

            var ans = new SolvingNode("T" + name, new Constrains(), SolvingNodeType.Named);
            _variables.Add(name, ans);
            return ans;
        }

        private void SetOrCreateLambda(int lambdaId, SolvingNode[] args,SolvingNode ret)
        {
            var fun = Fun.Of(args, ret);

            while (_syntaxNodes.Count <= lambdaId)
            {
                _syntaxNodes.Add(null);
            }

            var alreadyExists = _syntaxNodes[lambdaId];
            if (alreadyExists != null)
            {
                alreadyExists.State = SolvingFunctions.GetMergedState(fun, alreadyExists.State);
            }
            else
            {
                var res = new SolvingNode(lambdaId.ToString(), fun, SolvingNodeType.SyntaxNode);
                _syntaxNodes[lambdaId] = res;
            }
        }
        private SolvingNode SetOrCreatePrimitive(int id, Primitive type)
        {
            var node = GetOrCreateNode(id);
            if (!node.TryBecomeConcrete(type))
                throw new InvalidOperationException();
            return node;
        }
      
        private SolvingNode GetOrCreateArrayNode(int id, SolvingNode elementType)
        {
            while (_syntaxNodes.Count <= id)
            {
                _syntaxNodes.Add(null);
            }

            var alreadyExists = _syntaxNodes[id];
            if (alreadyExists != null)
            {
                alreadyExists.State = SolvingFunctions.GetMergedState(new Array(elementType), alreadyExists.State);
                return alreadyExists;
            }

            var res = new SolvingNode(id.ToString(), new Array(elementType), SolvingNodeType.SyntaxNode);
            _syntaxNodes[id] = res;
            return res;
        }
        private SolvingNode GetOrCreateNode(int id)
        {
            while (_syntaxNodes.Count <= id)
            {
                _syntaxNodes.Add(null);
            }

            var alreadyExists = _syntaxNodes[id];
            if (alreadyExists != null)
                return alreadyExists;

            var res = new SolvingNode(id.ToString(), new Constrains(), SolvingNodeType.SyntaxNode);
            _syntaxNodes[id] = res;
            return res;
        }

        private SolvingNode CreateVarType(IState state = null)
        {
            var varNode = new SolvingNode(
                name:  "V" + _varNodeId,
                state: state ?? new Constrains(),
                type:  SolvingNodeType.TypeVariable);
            _varNodeId++;
            _typeVariables.Add(varNode);
            return varNode;
        }


    }
}
