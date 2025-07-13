namespace Tasker.Cli.Helpers;

public static class ArgsParser
{
    public static (bool err, int arg) ParseSecondArg(string arg)
    {
        if (int.TryParse(arg, out int num))
            return (false, num);

        return (true, 0);
    }
}