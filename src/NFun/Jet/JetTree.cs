using System;
using System.Collections.Generic;
using System.Text;
using NFun.Types;

namespace NFun.Jet
{
    class JetTree
    {
        public JetFunDef[] UserFunctions { get; set; }
        public JetVarDefenition[] InputVariables { get; set; }
        public JetEquation[] JetEquations { get; set; }
    }

    public class JetFunDef
    {
        public string Name { get; set; }
        public VarType OutputType { get; set; }
        public Tuple<string,VarType> Arguments { get; set; }
        public object Body { get; set; }
    }

    public class JetCast
    {
        public VarType TargetType { get; set; }
        public object Expression { get; set; }
    }

    public class JetIfThenElse
    {
        public JetIfCase[] Cases { get; }
        public object ElseExpression { get; }

    }
    public class JetIfCase
    {
        public object Expression { get; set; }
        public object Check { get; set; }
    }
    public class JetArray
    {
        public object[] Elements { get; set; }
        public VarType ElementType { get; set; }

    }
    public class JetEquation
    {
        public string Name { get; set; }
        public VarType Type { get; set; }
        public object Expression { get; set; }

    }
    public class JetVarDefenition
    {
        public string Name { get; set; }
        public VarType Type { get; set; }

    }
    public class JetId {
        public string Name { get; set; }
    }

    public class JetConstant {
        public object Value{ get; set; }  
        public VarType Type { get; set; }
    }

    public class JetFunCall
    {
        public string NameAndTypes { get; }
        public object Arguments { get; }
        
    }
}
