public enum MechanicType
{
    Normal,
    Locked,
    Finish,
    NeedToWait,
    GoBackwards,
    GoToEnd
}


public struct LetterUnit
{
    public char character;   // the letter itself
    public int owner;        // 0 = Player A, 1 = Player B
    public MechanicType mechanic;
    public ushort param;


    public LetterUnit(char c, int owner, MechanicType m = MechanicType.Normal, ushort p = 0)
    {
        character = c; this.owner = owner; mechanic = m; param = p;
    }
}