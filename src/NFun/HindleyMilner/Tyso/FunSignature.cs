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

        public static FunSignature[] GetBestFits(FunSignature[] candidates, SolvingNode returnType,
            params SolvingNode[] argTypes)
        {
            bool severalAreSame = false;
            SignatureFit bestOne = new SignatureFit();
            var bestSignatures = new List<FunSignature>();
            bestOne.Failed = true;
            foreach (var candidate in candidates) {
                var returnTypeFit = returnType.CanBeConvertedFrom(candidate.ReturnType);
                //var returnTypeFit = SolvingNode.CreateStrict(candidate.ReturnType)
                //    .CanBeConvertedTo(returnType.MakeType()); 
                if (returnTypeFit.Type == FitType.Not)
                    continue;                

                var fit = new SignatureFit(returnTypeFit);
                
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

                if(fit.Failed)
                    continue;
                var compare = fit.IsBetterThan(bestOne);
                if (compare == 0)
                {
                    bestSignatures.Add(candidate);
                }
                else if (compare == 1)
                {
                    bestOne = fit;
                    bestSignatures.Clear();
                    bestSignatures.Add(candidate);
                }
            }
            return bestSignatures.ToArray();
        }
        public static FunSignature GetBestFitOrNull(FunSignature[] candidates, SolvingNode returnType,
            params SolvingNode[] argTypes)
        {
            var res = GetBestFits(candidates, returnType, argTypes);
            if (res.Length != 1)
                return null;
            return res.First();
        }

        private struct SignatureFit
        {

            public SignatureFit(FitResult returnResults)
            {
                ReturnTypeFit = returnResults;
                StrictFits = 0;
                CandidateFits = 0;
                ConvertedFits = 0;
                Failed = false;
                MaxCandidateDistance = 0;
                MaxConvertedDistance = 0;
            }
            public readonly FitResult ReturnTypeFit;
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
                
                if (MaxCandidateDistance < fit.MaxCandidateDistance)
                    return 1;
                if (fit.MaxCandidateDistance < MaxCandidateDistance)
                    return -1;
                
                if (ConvertedFits > fit.ConvertedFits)
                    return 1;
                if (ConvertedFits < fit.ConvertedFits)
                    return -1;
                
                if (MaxConvertedDistance < fit.MaxConvertedDistance)
                    return 1;
                if (fit.MaxConvertedDistance < MaxConvertedDistance)
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
                    
                
                //Ok. These signatures are completely the same. Error.
                return 0;
            }
        }
        
    }
    
    
}