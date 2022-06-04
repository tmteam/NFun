using System;

namespace NFun.Tic.Errors; 

public class TicException : Exception {
    protected TicException(string message) : base(message) { }
}