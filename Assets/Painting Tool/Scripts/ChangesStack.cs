
public class ChangesStack
{
    public struct Changes
    {
        public int SelectedAnim;
        public int SelectedLayer;
        public PaintingToolScript.PaintLayer layer;
    }

    protected int topPointer;
    protected int StackSize;

    protected Changes[] changeStack;

    //initialises stack
    public virtual void initialize(int Size)
    {
        StackSize = Size;
        topPointer = 0;
        changeStack = new Changes[StackSize];
    }

    //pushes a change to the stack and if it is full handles that in a separate function
    public virtual void push(Changes change)
    {
        if (changeStack == null)
        {
            return;
        }

        if (!IsFull())
        { 
            changeStack[topPointer] = change;
            topPointer++;
        }
        else
        {
            HandleFullStack(change);
        }
    }

    //returns the top item of the stack and decrements the top pointer
    public virtual Changes? pop()
    {
        if (!isEmpty())
        {
            topPointer--;
            return changeStack[topPointer];
        }
        return null;
    }

    //checks the top item of the stack
    public Changes? peek()
    {
        if (!isEmpty())
        {
            return changeStack[topPointer-1];
        }
        return null;
    }
    //checks if the stack is empty
    public bool isEmpty()
    {
        return changeStack == null || topPointer == 0;
    }
    //checks to see if stack limit has been reached
    public bool IsFull()
    {
        return topPointer == changeStack.Length;
    }

    //when changes at limit overwrites the bottom item of the stack
    private void HandleFullStack(Changes change)
    {
        for (int i = 1; i < changeStack.Length; i++)
        {
            changeStack[i-1] = changeStack[i];
        }
        topPointer--;
        push(change);
    }

    //resets stack reference
    public void nullStack()
    {
        topPointer = 0;
    }
}
