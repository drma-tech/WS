namespace WS.WEB.Shared
{
    public enum RenderControlStatus
    {
        Loading,
        Warning,
        Content,
        Error,
    }

    public sealed class RenderControlState<T> where T : class
    {
        public Func<string?, Task> StartLoading { get; set; }
        public Func<T?, Task> FinishLoading { get; set; }

        public Func<string?, Task> StartProcessing { get; set; }
        public Func<T?, Task> FinishProcessing { get; set; }

        public Func<string?, Task> ShowWarning { get; set; }
        public Func<string?, Task> ShowError { get; set; }

        public RenderControlStatus CurrentStatus { get; set; } = RenderControlStatus.Loading;
        public T? CurrentInstance { get; set; }
        public Func<T?, bool> ExpressionEmpty { get; set; }

        public string? MessageLoading { get; set; } = Translations.Notification.RenderControlLoading;
        public string? MessageError { get; set; }
        public string? MessageWarning { get; set; }

        public string? CustomMessageWarning { get; set; }
        public string? CustomMessageError { get; set; }
        public string? CustomPremiumDescription { get; set; }

        public Action? OnStateChanged { get; set; }

        public RenderControlState(Func<T?, bool> expressionEmpty)
        {
            ExpressionEmpty = expressionEmpty;

            StartLoading = async msg => await ChangeStatus(RenderControlStatus.Loading, msg);
            FinishLoading = async obj => await ChangeStatus(RenderControlStatus.Content, msg: null, obj);

            StartProcessing = async msg => await ChangeStatus(RenderControlStatus.Loading, msg ?? "Processing...");
            FinishProcessing = async obj => await ChangeStatus(RenderControlStatus.Content, msg: null, obj);

            ShowWarning = async msg => await ChangeStatus(RenderControlStatus.Warning, msg);
            ShowError = async msg => await ChangeStatus(RenderControlStatus.Error, msg);
        }

        private async Task ChangeStatus(RenderControlStatus status, string? msg = null, T? instance = default)
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
                await ChangeStatus(RenderControlStatus.Warning, Translations.Notification.RenderControlNoData);
                return;
            }

            CurrentStatus = status;
            CurrentInstance = instance;

            OnStateChanged?.Invoke();
        }
    }
}