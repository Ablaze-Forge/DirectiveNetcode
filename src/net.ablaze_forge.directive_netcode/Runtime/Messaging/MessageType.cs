namespace AblazeForge.DirectiveNetcode.Messaging
{
    public enum MessageType : byte
    {
        Default = 0,
        VarTracking = 1,
        Event = 2,
        Control = 3,
        Authentication = 4,

        // Reserved values to be replaced later, ensuring there's at least 4 bits to store the MessageType
        Reserved_1 = 5,
        Reserved_2 = 6,
        Reserved_3 = 7,
        Reserved_4 = 8,
        Reserved_5 = 9,
        Reserved_6 = 10,
        Reserved_7 = 11,
        Reserved_8 = 12,
        Reserved_9 = 13,
        Reserved_10 = 14,
        Reserved_11 = 15,
    }
}
