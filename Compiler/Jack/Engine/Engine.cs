
namespace Jack;

public class Engine : EngineBase, ICompilationEngine
{
    public Engine()
    {
        OnAdvance += __onAdvance;
        OnConsume += __onConsume;
        OnDefine += __onDefine;
        OnEnterGrammer += __onEnterGrammer;
        OnLeaveGrammer += __onLeaveGrammer;
    }

    #region event handler

    void __onAdvance(object? sender, EventArgs e)
    {
    }

    void __onConsume(object? sender, ConsumeEventArgs e)
    {
    }

    void __onDefine(object? sender, DefineEventArgs e)
    {
    }

    void __onEnterGrammer(object? sender, GrammerEventArgs e)
    {
    }

    void __onLeaveGrammer(object? sender, GrammerEventArgs e)
    {
    }

    #endregion

    #region EngineBase


    #endregion
}