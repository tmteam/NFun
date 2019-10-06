using System.Collections.Generic;
using System.IO;
using NFun.Types;

namespace Nfun.Fuspec.Parser
{
    public class InputText
    {
        public int Index => _index;
        public bool Eof => _listOfString.Count <= _index;
        public string CurrentLine => _listOfString[_index];
        private int _index=0;
        private List<string> _listOfString = new List<string>();
        
        public static InputText Read(StreamReader streamReader)
        {
            string line;
            var text = new InputText();
            while ((line = streamReader.ReadLine()) != null)
                text._listOfString.Add(line);

            text._index = 0;
            return text;
        }

        public void MoveNext()
        {
            if(Eof)
                return;
            _index++;
        }

        public bool IsCurentLineEmty() => CurrentLine.Trim() == "" ? true : false;
      //  {
        //    if (_cur.Trim() == "")
          //      return true;
           // else return false;
       // }
    }
}