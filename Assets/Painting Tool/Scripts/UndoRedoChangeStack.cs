
public class UndoRedoChangeStack : ChangesStack
{
    public ChangesStack Redo;

    public override void initialize(int Size)
    {
        base.initialize(Size);
        Redo = new ChangesStack();
        Redo.initialize(Size);
    }

    public override void push(Changes change)
    {
        Redo.nullStack();
        base.push(change);     
    }

    public override Changes? pop()
    {
        Changes? change  = base.pop();
        if (change != null)
        {
            Redopush((Changes)change);
        }
        return change;
    }

    public void Redopush(Changes change)
    {
        Redo.push(change);
    }

    //returns the top item of the stack and decrements the top pointer
    public Changes? Redopop()
    {
        return Redo.pop();
    }

    //checks the top item of the stack
    public Changes? Redopeek()
    {
        return Redo.peek();
    }
}
