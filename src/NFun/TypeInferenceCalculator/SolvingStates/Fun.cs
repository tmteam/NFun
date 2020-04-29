using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.Tic.SolvingStates
{
    public class Fun : ICompositeType, IType, IState
    {
        public static Fun Of(IState[] argTypes, IState returnType)
        {
            SolvingNode[] argNodes = new SolvingNode[argTypes.Length];
            SolvingNode retNode = null;

            if (returnType is IType rt)
                retNode = SolvingNode.CreateTypeNode(rt);
            else if (returnType is RefTo retRef)
                retNode = retRef.Node;
            else
                throw new InvalidOperationException();

            for (int i = 0; i < argTypes.Length; i++)
            {
                if (argTypes[i] is IType at)
                    argNodes[i] = SolvingNode.CreateTypeNode(at);
                else if (argTypes[i] is RefTo aRef)
                    argNodes[i] = aRef.Node;
                else
                    throw new InvalidOperationException();
            }
            

            return new Fun(argNodes,retNode);
        }
        public static Fun Of(IState argType,IState returnType) 
            => Of(new[] { argType },returnType );

        public static Fun Of(IType[] argTypes,IType retType)
        {
            return new Fun(
                argNodes: argTypes.Select(SolvingNode.CreateTypeNode).ToArray(),
                retNode: SolvingNode.CreateTypeNode(retType));
        }
        public static Fun Of(SolvingNode[] argNodes,SolvingNode returnNode)
            => new Fun(argNodes,returnNode);
        public static Fun Of(SolvingNode argNode,SolvingNode returnNode) 
            => new Fun(new []{argNode},returnNode);

        private Fun(SolvingNode[] argNodes,SolvingNode retNode)
        {
            ArgNodes = argNodes;
            RetNode = retNode;
        }

        public IState ReturnType => RetNode.State;
        public IState GetArgType(int index) => ArgNodes[index].State;
        public SolvingNode RetNode { get; }
        public SolvingNode[] ArgNodes { get; }
        public IEnumerable<IState> Args => ArgNodes.Select(a => a.State);

        public int ArgsCount => ArgNodes.Length;
        public bool IsSolved => RetNode.IsSolved && ArgNodes.All(n=>n.IsSolved);
        public IType GetLastCommonAncestorOrNull(IType otherType)
        {
            var funType = otherType as Fun;
            
            if (funType == null)
                return Primitive.Any;

            if (funType.ArgsCount != ArgsCount)
                return Primitive.Any;

            if (!(ReturnType is IType returnType))
                return null;
            if (!(funType.ReturnType is IType returnTypeB))
                return null;
            if (!returnType.IsSolved || !returnTypeB.IsSolved)
                return null;
            
            var returnAnc = returnType.GetLastCommonAncestorOrNull(returnTypeB);

            IType[] argTypes = new IType[ArgsCount];

            for (int i = 0; i < ArgsCount; i++)
            {
                var aArg = GetArgType(i);
                var bArg = funType.GetArgType(i);
                if (!(aArg is IType typeA && bArg is IType typeB))
                    return null;


                if (!(typeA.IsSolved && typeB.IsSolved))
                    return null;

                if(typeA.Equals(typeB))
                        argTypes[i] = typeA;
                else if (aArg is Primitive primitiveA && bArg is Primitive primitiveB)
                {
                    var argDesc = primitiveA.GetFirstCommonDescendantOrNull(primitiveB);
                    if (argDesc == null)
                        return null;
                    argTypes[i] = argDesc;
                }
                else return null;
            }
            return Of(retType: returnAnc, argTypes: argTypes);

        }

        public bool CanBeImplicitlyConvertedTo(Primitive type) 
            => type.Equals(Primitive.Any);

        public override bool Equals(object obj)
        {
            if (!(obj is Fun fun))
                return false;
            if(fun.ArgsCount != ArgsCount)
                return false;

            for (int i = 0; i < ArgsCount; i++)
            {
                if (!fun.GetArgType(i).Equals(GetArgType(i)))
                    return false;
            }

            return fun.ReturnType.Equals(ReturnType);
        }

        public ICompositeType GetNonReferenced() 
            => new Fun(ArgNodes.Select(a=>a.GetNonReference()).ToArray(), RetNode.GetNonReference());

        public IEnumerable<SolvingNode> Members => ArgNodes.Append(RetNode);

        public IEnumerable<SolvingNode> AllLeafTypes
        {
            get
            {
                foreach (var member in Members)
                {
                    if (member.State is ICompositeType composite)
                    {
                        foreach (var leaf in composite.AllLeafTypes)
                        {
                            yield return leaf;
                        }
                    }
                    else
                    {
                        yield return member;
                    }
                }
            }
        }

        public override string ToString()
        {
            if(ArgsCount==1)
                return $"({GetArgType(0)}->{ReturnType})";
            return $"(({string.Join(",", ArgNodes.Select(a=>a.State))})->{ReturnType})";

        }

        public string Description => $"({string.Join(",", ArgNodes.Select(a => a.Name))})->{RetNode.Name}";
    }
}