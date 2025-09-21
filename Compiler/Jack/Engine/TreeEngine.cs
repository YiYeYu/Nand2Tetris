
namespace Jack;

public class TreeEngine : EngineBase, ICompilationEngine
{
    public class TreeNode
    {
        public delegate void DVisitNode(TreeNode node);

        public interface IVisitor
        {
            /// <summary>
            /// 前序访问当前节点，非终结符
            /// </summary>
            /// <param name="node"></param>
            void VisitDown(TreeNode node);
            /// <summary>
            /// 访问当前节点，终结符
            /// </summary>
            /// <param name="node"></param>
            void Visit(TreeNode node);
            /// <summary>
            /// 后序访问当前节点，非终结符
            /// </summary>
            void VisitUp(TreeNode node);
        }

        public class Visitor : IVisitor
        {
            public DVisitNode? DVisit { get; set; }
            public DVisitNode? DVisitDown { get; set; }
            public DVisitNode? DVisitUp { get; set; }

            public void Visit(TreeNode node)
            {
                DVisit?.Invoke(node);
            }

            public void VisitDown(TreeNode node)
            {
                DVisitDown?.Invoke(node);
            }

            public void VisitUp(TreeNode node)
            {
                DVisitUp?.Invoke(node);
            }
        }

        public Grammer Grammer { get; private set; }
        public string Token { get; set; }
        public TreeNode? Parent { get; set; }
        public IList<TreeNode> Children { get; private set; }

        public TreeNode(Grammer grammer, string token = "", TreeNode? parent = null)
        {
            Parent = parent;
            Grammer = grammer;
            Token = token;
            Children = new List<TreeNode>();
        }

        public void AddChild(TreeNode node) => Children.Add(node);
        public TreeNode GetChild(int index) => Children[index];

        public void Visit(IVisitor vistor)
        {
            if (Children.Count == 0)
            {
                vistor.Visit(this);
                return;
            }

            vistor.VisitDown(this);

            foreach (var child in Children)
            {
                child.Visit(vistor);
            }

            vistor.VisitUp(this);
        }
    }

    static readonly Dictionary<Grammer, bool> _isTerminal = new()
    {
        { Grammer.Identifier, true },
        { Grammer.Keyword, true },
        { Grammer.Symbol, true },
        { Grammer.IntegerConstant, true },
        { Grammer.StringConstant, true },
    };

    public TreeNode? Root
    { get; private set; }
    List<TreeNode> nodeStack = new();

    public TreeEngine()
    {
        OnStart += __onStart;
        OnEnd += __onEnd;

        OnEnterGrammer += __onEnterGrammer;
        OnLeaveGrammer += __onLeaveGrammer;
    }

    #region event handler

    void __onStart(object? sender, EventArgs e)
    {
        Root = null;
    }

    void __onEnd(object? sender, EventArgs e)
    {
    }

    protected virtual void __onEnterGrammer(object? sender, GrammerEventArgs e)
    {
        if (IsTerminal(e.Grammer))
        {
            return;
        }

        PushNode(e.Grammer, parser.Token());
    }

    protected virtual void __onLeaveGrammer(object? sender, GrammerEventArgs e)
    {
        if (IsTerminal(e.Grammer))
        {
            return;
        }

        PopNode();
    }

    #endregion

    #region EngineBase

    // 5种终结符

    protected override void MatchKeyword(string str)
    {
        base.MatchKeyword(str);

        AddChildNode(Grammer.Keyword, str);
    }

    protected override void MatchSymbol(string c)
    {
        base.MatchSymbol(c);

        AddChildNode(Grammer.Symbol, c);
    }

    protected override void MatchIdentifier()
    {
        base.MatchIdentifier();

        AddChildNode(Grammer.Identifier, parser.Token());
    }

    protected override void MatchIntegerConstant()
    {
        base.MatchIntegerConstant();

        AddChildNode(Grammer.IntegerConstant, parser.Token());
    }

    protected override void MatchStringConstant()
    {
        base.MatchStringConstant();

        AddChildNode(Grammer.StringConstant, parser.Token());
    }

    // 程序结构

    protected override void CompileClassName()
    {
        base.CompileClassName();

        PeekNode().Token = LastIdentifier;
    }

    protected override void CompileSubroutineName()
    {
        base.CompileSubroutineName();

        PeekNode().Token = LastIdentifier;
    }

    protected override void CompileVarName()
    {
        base.CompileVarName();

        PeekNode().Token = LastIdentifier;
    }

    // 语句

    // 表达式

    protected override void CompileSubroutineCall()
    {
        Advandce();
        PeekNode().Token = parser.Token();
        base.CompileSubroutineCall();
    }


    #endregion

    #region utils

    protected TreeNode PushNode(Grammer grammer, string token = "")
    {
        Console.WriteLine($"PushNode: {grammer}, {token}");

        TreeNode node = new(grammer, token, SafePeekNode());
        nodeStack.Add(node);
        return node;
    }

    protected TreeNode AddChildNode(Grammer grammer, string token = "")
    {
        TreeNode node = new(grammer, token);
        PeekNode().AddChild(node);
        return node;
    }

    protected TreeNode PopNode()
    {
        TreeNode node = nodeStack.Pop();

        Console.WriteLine($"PopNode: {node.Grammer}, {node.Token}, parent {SafePeekNode()?.Grammer}, {SafePeekNode()?.Token}");

        if (nodeStack.Empty())
        {
            Root = node;
            return node;
        }

        TreeNode parent = nodeStack.Peek();
        parent.AddChild(node);

        return node;
    }

    protected TreeNode PeekNode() => nodeStack.Peek();
    protected TreeNode? SafePeekNode() => nodeStack.Empty() ? null : nodeStack.Peek();

    static bool IsTerminal(Grammer grammer) => _isTerminal.ContainsKey(grammer);

    #endregion
}