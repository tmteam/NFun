using System.Collections.Generic;

namespace Nfun.Fuspec.Parser.Model
{
    public class SetCheckKit
    {
        public List< string> SetKit { get; }
        public List<string> CheckKit { get; }

        public SetCheckKit()
        {
            SetKit=new List<string>();
            CheckKit = new List<string>();
        }

        public void AddSet(string setString)
        {
            SetKit.Add(setString);
        }

        public void AddSetKit(List<string> setKit)
        {
            SetKit.AddRange(setKit);
        }

        public void AddCheck(string answer)
        {
            CheckKit.Add(answer);
        }
        
        public void AddCheckKit(List<string> setKit)
        {
            CheckKit.AddRange(setKit);
        }
    }
}