namespace TestCom
{
    internal class ExceptionHelper
    {
        public static void ThrowEx(string message, params string[] args)
        {
            var errorMessage = args.Length != 0
                ? String.Format(message, args)
                : message;

            throw new ApplicationException(errorMessage);
        }
    }
}
