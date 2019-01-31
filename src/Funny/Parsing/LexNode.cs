using System.Collections.Generic;
using System.Linq;
using Funny.Tokenization;

namespace Funny.Parsing
{
    public class LexNode
    {
        public LexNode(Tok token, params LexNode[] children)
        {
            ChildrenNode = children;
            Op = token;
        }

        public Tok Op { get; }

        public int Finish { get; set; }
        public  IEnumerable<LexNode> ChildrenNode { get; }
        public override string ToString()
        {
            if(!ChildrenNode.Any())
                return Op.ToString();
            else
            {
                return $"{Op}( {string.Join(',', ChildrenNode.Select(c => c.ToString()))})";
            }
        }
    }
}