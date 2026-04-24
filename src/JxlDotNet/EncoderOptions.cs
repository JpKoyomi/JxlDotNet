namespace JxlDotNet;

public readonly record struct EncoderOptions(
    float Distance = 1.0f,
    int Effort = 7,
    bool Lossless = false);
