namespace System.Threading.Tasks
{
    public static class ActionExtensions
    {
        public static Task InvokeAsync(
            this Action @this) => MulticastDelegateExtensions.MulticastInvokeAsync(
                @this,
                true,
                true);

        public static Task InvokeAsync<T>(
            this Action<T> @this,
            T arg1) => MulticastDelegateExtensions.MulticastInvokeAsync(
                @this,
                true,
                true,
                arg1);

        public static Task InvokeAsync<T1, T2>(
            this Action<T1, T2> @this,
            T1 arg1,
            T2 arg2) => MulticastDelegateExtensions.MulticastInvokeAsync(
                @this,
                true,
                true,
                arg1,
                arg2);

        public static Task InvokeAsync<T1, T2, T3>(
            this Action<T1, T2, T3> @this,
            T1 arg1,
            T2 arg2,
            T3 arg3) => MulticastDelegateExtensions.MulticastInvokeAsync(
                @this,
                true,
                true,
                arg1,
                arg2,
                arg3);
    }
}