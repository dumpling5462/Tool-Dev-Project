using System.Collections.Generic;

public class ChangesStack
{
    public struct Changes
    {
        public int SelectedAnim;
        public int SelectedLayer;
        public PaintingToolScript.PaintLayer layer;
        public List<PaintingToolScript.PaintLayer> Frame;
        public List<PaintingToolScript.PaintLayer> Layers;
        public bool Delete;
        public bool Added;
        public bool Move;
        public int NewIndex;
        public int OldIndex;
    }

    protected int topPointer;
    protected int StackSize;

    protected Changes[] changeStack;

    //initialises stack
    public virtual void initialise(int Size)
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
    public virtual Changes? pop(Changes? oldstate)
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
        return changeStack == null || topPointer <= 0;
    }
    //checks to see if stack limit has been reached
    public bool IsFull()
    {
        return topPointer == changeStack.Length;
    }

    //when changes at limit overwrites the bottom item of the stack
    private void HandleFullStack(Changes change)
    {
        if (changeStack[0].layer.LayerImage != null)
            UnityEngine.Object.DestroyImmediate(changeStack[0].layer.LayerImage);
        if (changeStack[0].Frame != null)
        {
            foreach (PaintingToolScript.PaintLayer Layer in changeStack[0].Frame)
            {
                if (Layer.LayerImage != null)
                    UnityEngine.Object.DestroyImmediate(Layer.LayerImage);
            }
        }
        if (changeStack[0].Layers != null)
        {
            foreach (PaintingToolScript.PaintLayer Layer in changeStack[0].Layers)
            {
                if (Layer.LayerImage != null)
                    UnityEngine.Object.DestroyImmediate(Layer.LayerImage);
            }
        }

        changeStack[0] = new Changes();
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
        for (int i = 0; i < changeStack.Length; i++)
        {
            if (changeStack[i].layer.LayerImage != null)
            {
                UnityEngine.Object.DestroyImmediate(changeStack[i].layer.LayerImage);
            }
            changeStack[i] = new Changes();
        }
        topPointer = 0;
    }
    public int getPointer()
    {
        return topPointer;
    }
}
