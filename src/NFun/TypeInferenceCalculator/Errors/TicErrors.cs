using System;
using System.Collections.Generic;
using System.Text;
using NFun.Tic;
using NFun.Tic.SolvingStates;

namespace NFun.TypeInferenceCalculator.Errors
{
    public class TicNoDetailsError : TicException
    {
        public TicNoDetailsError() : base("Unknown tic error")
        {
        }
    }
    public static class TicErrors
    {
        public static Exception IncompatibleNodes(SolvingNode ancestor, SolvingNode descendant)
        {
            return new TicNoDetailsError();
        }
        public static Exception IncompatibleTypes(SolvingNode ancestor, SolvingNode descendant)
        {
            return new TicNoDetailsError();
        }
        public static Exception CanntoBecomeFunction(SolvingNode ancestor, SolvingNode target)
        {
            return new TicNoDetailsError();
        }
        public static Exception CanntoBecomeArray(SolvingNode ancestor, SolvingNode target)
        {
            return new TicNoDetailsError();
        }
        public static Exception IncompatibleFunSignatures(SolvingNode ancestor, SolvingNode descendant)
        {
            return new TicNoDetailsError();
        }
        public static Exception IncompatibleStates(IState ancestor, IState descendant)
        {
            return new TicNoDetailsError();
        }

        public static Exception CannotBecome(SolvingNode targetNode, IType type)
        {
            return new TicNoDetailsError();
        }

        public static Exception CannotMerge(SolvingNode a, SolvingNode b)
        {
            return new TicNoDetailsError();

        }
        public static Exception CannotMergeGroup(SolvingNode[] group, SolvingNode a, SolvingNode b)
        {
            return new TicNoDetailsError();

        }
        public static Exception RecursiveTypeDefenition(SolvingNode[] group)
        {
            return new TicNoDetailsError();

        }
        public static Exception CannotSetState(SolvingNode a, IState b)
        {
            return new TicNoDetailsError();

        }
    }
}
