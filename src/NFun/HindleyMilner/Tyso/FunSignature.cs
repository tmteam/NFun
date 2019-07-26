using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.HindleyMilner.Tyso
{
    public class FunSignature
    {
        public CallDef ToCallDefenition(int returnNodeId, params int[] argIds)
        {
            return new CallDef(new[]{ReturnType}.Concat(ArgTypes).ToArray(), new[]{returnNodeId}.Concat(argIds).ToArray());
        }
        public readonly FType ReturnType;
        public readonly FType[] ArgTypes;

        public FunSignature(FType returnType, params FType[] argTypes)
        {
            ReturnType = returnType;
            ArgTypes = argTypes;
        }

        public override string ToString() 
            => $"({string.Join(",", ArgTypes.Select(a => a.ToString()))})->{ReturnType}";

        public static FunSignature GetBestFitOrNull(FunSignature[] candidates, SolvingNode returnType,
            params SolvingNode[] argTypes)
        {
            bool severalAreSame = false;
            SignatureFit bestOne = new SignatureFit();
            bestOne.Failed = true;
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
            
                var returnTypeFit = SolvingNode.CreateStrict(candidate.ReturnType)
                    .CanBeConvertedTo(returnType.MakeType()); 
                
                if (returnTypeFit.Type == FitType.Not)
                    continue;                

                var fit = new SignatureFit(returnTypeFit,i);
                
                for (int argNum = 0; argNum < argTypes.Length; argNum++)
                {
                    var candidateArg = candidate.ArgTypes[argNum];
                    var actualArg = argTypes[argNum];
                    
                    var argFit =  actualArg.CanBeConvertedTo(candidateArg);
                    if (argFit.Type == FitType.Not)
                    {
                        fit.Failed = true;
                        break;
                    } 
                    if (argFit.Type == FitType.Strict)
                        fit.StrictFits++;
                    else if (argFit.Type == FitType.Candidate)
                    {
                        fit.CandidateFits++;
                        fit.MaxCandidateDistance = Math.Max(argFit.Distance, fit.MaxCandidateDistance);
                    }
                    else if (argFit.Type == FitType.Converable)
                    {
                        fit.ConvertedFits++;
                        fit.MaxConvertedDistance = Math.Max(argFit.Distance, fit.MaxCandidateDistance);
                    }
                }

                var compare = fit.IsBetterThan(bestOne);
                if (compare == 0)
                    severalAreSame = true;
                else if (compare == 1)
                {
                    bestOne = fit;
                    severalAreSame = false;
                }
            }

            if (severalAreSame)
                return null;
            if (bestOne.Failed)
                return null;

            return candidates[bestOne.Index];
        }

        private struct SignatureFit
        {

            public SignatureFit(FitResult returnResults, int index)
            {
                ReturnTypeFit = returnResults;
                Index = index;
                StrictFits = 0;
                CandidateFits = 0;
                ConvertedFits = 0;
                Failed = false;
                MaxCandidateDistance = 0;
                MaxConvertedDistance = 0;
            }
            public readonly FitResult ReturnTypeFit;
            public readonly int Index;
            public int MaxCandidateDistance;
            public int MaxConvertedDistance;
            public int StrictFits;
            public int CandidateFits;
            public int ConvertedFits;
            public bool Failed;

            public int IsBetterThan(SignatureFit fit)
            {
                if (Failed)
                    return fit.Failed ? 0 : -1;
                if (fit.Failed)
                    return 1;
                if (StrictFits > fit.StrictFits)
                    return 1;
                if (StrictFits < fit.StrictFits)
                    return -1;
                if (CandidateFits > fit.CandidateFits)
                    return 1;
                if (CandidateFits < fit.CandidateFits)
                    return -1;
                if (ConvertedFits > fit.ConvertedFits)
                    return 1;
                if (ConvertedFits < fit.ConvertedFits)
                    return -1;

                //Compare by output type:
                if (ReturnTypeFit.Type == FitType.Strict) { 
                    if (fit.ReturnTypeFit.Type != FitType.Strict) 
                        return 1;
                }
                else {
                    if (fit.ReturnTypeFit.Type == FitType.Strict)
                        return -1;
                    if (ReturnTypeFit.Type == FitType.Candidate) {
                        if (fit.ReturnTypeFit.Type != FitType.Candidate)
                            return 1;
                    }
                    else {
                        if (fit.ReturnTypeFit.Type == FitType.Candidate)
                            return -1;
                    }
                }
                //Output types are equal. Need to compare convert closeness

                if (MaxCandidateDistance < fit.MaxCandidateDistance)
                    return 1;
                if (fit.MaxCandidateDistance < MaxCandidateDistance)
                    return -1;
                
                if (MaxConvertedDistance < fit.MaxConvertedDistance)
                    return 1;
                if (fit.MaxConvertedDistance < MaxConvertedDistance)
                    return -1;
                    
                
                //Ok. These signatures are completely the same. Error.
                return 0;
            }
        }
        public static FunSignature GetBestFitOrNullOld(FunSignature[] candidates, SolvingNode returnType, params SolvingNode[] argTypes)
        {
            FunSignature bestCandidate = null;
            bool severalNonStrictFits = false;
            int fitScore = 0;
            for (var i = 0; i < candidates.Length; i++)
            {
                var candidate = candidates[i];
                FitType candidateConvert;
                int candidateScore = 0;
                var returnTypeFit = returnType
                    .CanBeConvertedTo(candidate.ReturnType, SolvingNode.MaxTypeDepth);
                if (returnTypeFit.Type == FitType.Not)
                    continue;
                candidateScore += (int) returnTypeFit.Type;
                candidateConvert = returnTypeFit.Type;

                for (int argNum = 0; argNum < argTypes.Length; argNum++)
                {
                    var argFit = argTypes[argNum].CanBeConvertedTo(
                        candidate.ArgTypes[argNum], SolvingNode.MaxTypeDepth);
                    if (argFit.Type == FitType.Not)
                        continue;
                    if (argFit.Type < candidateConvert)
                        candidateConvert = argFit.Type;
                    candidateScore += (int) argFit.Type;
                }

                if (candidateConvert == FitType.Strict)
                {
                    severalNonStrictFits = false;
                    return candidate;
                }

                if (candidateConvert == FitType.Converable)
                {
                    if (bestCandidate != null && candidateScore == fitScore)
                        severalNonStrictFits = true;
                    else
                    {
                        if (candidateScore > fitScore)
                        {
                            fitScore = candidateScore;
                            severalNonStrictFits = false;
                            bestCandidate = candidate;
                        }
                    }
                }
            }

            if (severalNonStrictFits)
                return null;
            return bestCandidate;
        }
    }
    
    
}