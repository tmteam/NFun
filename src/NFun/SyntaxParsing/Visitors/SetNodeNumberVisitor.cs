namespace NFun.SyntaxParsing.Visitors; 

public class SetNodeNumberVisitor : EnterVisitorBase {
    private int _lastNum;
    public int LastUsedNumber => _lastNum;

    public SetNodeNumberVisitor(int startNum) { _lastNum = startNum; }

    protected override DfsEnterResult DefaultVisitEnter(ISyntaxNode node) {
        node.OrderNumber = _lastNum++;
        return DfsEnterResult.Continue;
    }
}