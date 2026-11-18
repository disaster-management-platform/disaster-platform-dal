namespace DisasterLocation.Core;

public enum DisasterPhase
{
    Registered = 0, Response = 1, Recovery = 2, Closed = 3,
    Detected = 4, Assessment = 5, Reconstruction = 6
}

public enum AttentionLevel { Low = 1, Medium = 2, High = 3, Critical = 4 }
