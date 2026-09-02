namespace WS.WEB.Shared
{
    public enum RenderControlStatus
    {
        Loading,
        Warning,
        Content,
        Error,
    }

    public sealed class RenderControlState<T>
    {
        public Func<string?, Task> StartLoading { get; set; }
        public Func<T, Task> FinishLoading { get; set; }

        public Func<string?, Task> StartProcessing { get; set; }
        public Func<T, Task> FinishProcessing { get; set; }

        public Func<string?, Task> ShowWarning { get; set; }
        public Func<string?, Task> ShowError { get; set; }

        public RenderControlStatus Status { get; set; } = RenderControlStatus.Loading;
        public T Instance { get; set; }
        public Func<T, bool> ExpressionEmpty { get; set; }

        public string? MessageLoading { get; set; } = Translations.Notification.RenderControlLoading;
        public string? MessageError { get; set; }
        public string? MessageWarning { get; set; }

        public string? CustomMessageWarning { get; set; }
        public string? CustomMessageError { get; set; }
        public string? CustomPremiumDescription { get; set; }

        public Action? OnStateChanged { get; set; }

        public RenderControlState(T initialValue, Func<T, bool> expressionEmpty)
        {
            Instance = initialValue;
            ExpressionEmpty = expressionEmpty;

            StartLoading = async msg => await ChangeStatus(RenderControlStatus.Loading, initialValue, msg);
            FinishLoading = async obj => await ChangeStatus(RenderControlStatus.Content, obj, msg: null);

            StartProcessing = async msg => await ChangeStatus(RenderControlStatus.Loading, initialValue, msg ?? "Processing...");
            FinishProcessing = async obj => await ChangeStatus(RenderControlStatus.Content, obj, msg: null);

            ShowWarning = async msg => await ChangeStatus(RenderControlStatus.Warning, initialValue, msg);
            ShowError = async msg => await ChangeStatus(RenderControlStatus.Error, initialValue, msg);
        }

        private async Task ChangeStatus(RenderControlStatus status, T instance, string? msg = null)
        {
            if (status == RenderControlStatus.Loading)
            {
                MessageLoading = msg ?? Translations.Notification.RenderControlLoading;
            }
            else if (status == RenderControlStatus.Warning)
            {
                MessageWarning = CustomMessageWarning ?? msg;
            }
            else if (status == RenderControlStatus.Error)
            {
                MessageError = CustomMessageError ?? msg;
            }
            else if (status == RenderControlStatus.Content && (Equals(instance, default(T)) || ExpressionEmpty(instance)) && CustomMessageWarning.NotEmpty())
            {
                await ChangeStatus(RenderControlStatus.Warning, instance, Translations.Notification.RenderControlNoData);
                return;
            }

            Status = status;
            Instance = instance;

            OnStateChanged?.Invoke();
        }
    }
}