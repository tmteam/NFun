using System;
using System.Collections.Generic;
using System.Linq;

namespace NFun.TypeInference.Solving
{
    public class TiFunctionSignature
    {
        public CallDefenition ToCallDefenition(int returnNodeId, params int[] argIds)
        {
            return new CallDefenition(
                new[]{ReturnType}.Concat(ArgTypes).ToArray(), 
                new[]{returnNodeId}.Concat(argIds).ToArray());
        }
        public readonly TiType ReturnType;
        public readonly TiType[] ArgTypes;

        public TiFunctionSignature(TiType returnType, params TiType[] argTypes)
        {
            ReturnType = returnType;
            ArgTypes = argTypes;
        }

        public override string ToString() 
            => $"({string.Join(",", ArgTypes.Select(a => a.ToString()))})->{ReturnType}";

        public static TiFunctionSignature[] GetBestFits(TiFunctionSignature[] candidates, SolvingNode returnType,
            params SolvingNode[] argTypes)
        {
            SignatureFit bestOne = new SignatureFit();
            var bestSignatures = new List<TiFunctionSignature>();
            foreach (var candidate in candidates) 
            {
                var returnTypeFit = returnType.CanBeConvertedFrom(candidate.ReturnType);
                if (returnTypeFit.Type == FitType.Not)
                    continue;                

                var fit = new SignatureFit(returnTypeFit);
                
                for (int argNum = 0; argNum < argTypes.Length; argNum++)
                {
                    var candidateArg = candidate.ArgTypes[argNum];
                    var actualArg = argTypes[argNum];
                    
                    var argFit =  actualArg.CanBeConvertedTo(candidateArg);
                    fit.Append(argFit); 
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
        
      
        public static TiFunctionSignature GetBestFitOrNull(TiFunctionSignature[] candidates, SolvingNode returnType,
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
                _returnTypeFit = returnResults;
                _strictFits = 0;
                _candidateFits = 0;
                _convertedFits = 0;
                Failed = false;
                _maxCandidateDistance = 0;
                _maxConvertedDistance = 0;
            }

            public void Append(FitResult fitResult)
            {
                if (fitResult.Type == FitType.Not)
                {
                    Failed = true;
                    return;
                }

                if (fitResult.Type == FitType.Strict)
                    _strictFits++;
                else if (fitResult.Type == FitType.Candidate)
                {
                    _candidateFits++;
                    _maxCandidateDistance = Math.Max(fitResult.Distance, _maxCandidateDistance);
                }
                else if (fitResult.Type == FitType.Convertable)
                {
                    _convertedFits++;
                    _maxConvertedDistance = Math.Max(fitResult.Distance, _maxCandidateDistance);
                }
            }

            private readonly FitResult _returnTypeFit;
            private int _maxCandidateDistance;
            private int _maxConvertedDistance;
            private int _strictFits;
            private int _candidateFits;
            private int _convertedFits;
            public bool Failed { get; private set; }

            public int IsBetterThan(SignatureFit fit)
            {
                if (Failed)
                    return fit.Failed ? 0 : -1;
                if (fit.Failed)
                    return 1;
                if (_strictFits > fit._strictFits)
                    return 1;
                if (_strictFits < fit._strictFits)
                    return -1;
                if (_candidateFits > fit._candidateFits)
                    return 1;
                if (_candidateFits < fit._candidateFits)
                    return -1;
                
                
                if (_convertedFits > fit._convertedFits)
                    return 1;
                if (_convertedFits < fit._convertedFits)
                    return -1;
                
                //Compare by output type:
                if (_returnTypeFit.Type == FitType.Strict) { 
                    if (fit._returnTypeFit.Type != FitType.Strict) 
                        return 1;
                }
                else{
                    if (fit._returnTypeFit.Type == FitType.Strict)
                        return -1;
                }
                
                //Compare by output type:
                if (_returnTypeFit.Type != FitType.Strict) { 
                    if (_returnTypeFit.Type == FitType.Candidate) {
                        if (fit._returnTypeFit.Type != FitType.Candidate)
                            return 1;
                    }
                    else {
                        if (fit._returnTypeFit.Type == FitType.Candidate)
                            return -1;
                    }
                }
                
                if (_maxCandidateDistance < fit._maxCandidateDistance)
                    return 1;
                if (fit._maxCandidateDistance < _maxCandidateDistance)
                    return -1;
                
                if (_maxConvertedDistance < fit._maxConvertedDistance)
                    return 1;
                if (fit._maxConvertedDistance < _maxConvertedDistance)
                    return -1;

                    
                
                //Ok. These signatures are completely the same. Error.
                return 0;
            }
        }
    }
}