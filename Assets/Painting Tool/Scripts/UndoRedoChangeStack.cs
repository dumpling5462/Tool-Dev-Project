
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
    public void pushRedo(Changes change)
    {
        Redo.push(change);
    }

    public override Changes? pop(Changes? oldState)
    {
        Changes? change = base.pop(null);
        if (change != null /*&& oldState != null*/)
        {
            //pushRedo((Changes)oldState);
            pushRedo(change.Value);
        }
        return change;
    }

    public void Redopush(Changes change)
    {
        base.push(change);
    }

    //returns the top item of the stack and decrements the top pointer
    public Changes? Redopop()
    {
        return Redo.pop(null);
    }

    //checks the top item of the stack
    public Changes? Redopeek()
    {
        return Redo.peek();
    }
}
