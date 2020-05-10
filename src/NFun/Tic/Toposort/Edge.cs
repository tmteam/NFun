using System;

namespace NFun.Tic.Toposort
{
    public enum EdgeType
    {
        Root,
        Ancestor,
        Equal,
        Member
    }
    public struct Edge
    {
        public readonly int To;
        public readonly EdgeType Type;
        public static Edge AncestorTo(int to) => new Edge(to, EdgeType.Ancestor);
        public static Edge ReferenceTo(int to) => new Edge(to, EdgeType.Equal);
        public static Edge RootOf(int to) => new Edge(to, EdgeType.Root);
        public static Edge MemberOf(int to) => new Edge(to, EdgeType.Member);


        public Edge(int to, EdgeType type)
        {
            To = to;
            Type = type;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case EdgeType.Root:     return "!!!" + To;
                case EdgeType.Ancestor: return "::>"+To;
                case EdgeType.Equal:    return "<=>"+To;
                case EdgeType.Member: return "-->" + To;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Edge e))
                return false;
            return Equals(e);
        }

        public bool Equals(Edge other) 
            => To == other.To && Type == other.Type;

        public override int GetHashCode()
        {
            unchecked
            {
                return (To * 397) ^ (int) Type;
            }
        }
    }
}