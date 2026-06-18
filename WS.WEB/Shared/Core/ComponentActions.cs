namespace WS.WEB.Shared.Core
{
    public enum RenderStatus
    {
        Loading,
        Warning,
        Content,
        Error,
    }

    public class ComponentActions<T>
    {
        public Func<string?, Task> StartLoading { get; set; }
        public Func<T?, Task> FinishLoading { get; set; }

        public Func<string?, Task> StartProcessing { get; set; }
        public Func<T?, Task> FinishProcessing { get; set; }

        public Func<string?, Task> ShowWarning { get; set; }
        public Func<string?, Task> ShowError { get; set; }

        public RenderStatus CurrentStatus { get; set; } = RenderStatus.Loading;
        public T? CurrentInstance { get; set; }
        public Func<T?, bool> ExpressionEmpty { get; set; }

        public string? MessageLoading { get; set; } = GlobalTranslations.CustomVisibilityLoading;
        public string? MessageError { get; set; }
        public string? MessageWarning { get; set; }

        public string? CustomMessageWarning { get; set; }
        public string? CustomMessageError { get; set; }
        public string? CustomPremiumDescription { get; set; }

        public Action? OnStateChanged { get; set; }

        public ComponentActions(Func<T?, bool> expressionEmpty)
        {
            ExpressionEmpty = expressionEmpty;

            StartLoading = async msg => await ChangeStatus(RenderStatus.Loading, msg);
            FinishLoading = async obj => await ChangeStatus(RenderStatus.Content, null, obj);

            StartProcessing = async msg => await ChangeStatus(RenderStatus.Loading, msg ?? "Processing...");
            FinishProcessing = async obj => await ChangeStatus(RenderStatus.Content, null, obj);

            ShowWarning = async msg => await ChangeStatus(RenderStatus.Warning, msg);
            ShowError = async msg => await ChangeStatus(RenderStatus.Error, msg);
        }

        private async Task ChangeStatus(RenderStatus status, string? msg = null, T? instance = default)
        {
            if (status == RenderStatus.Loading)
            {
                MessageLoading = msg ?? GlobalTranslations.CustomVisibilityLoading;
            }
            else if (status == RenderStatus.Warning)
            {
                MessageWarning = CustomMessageWarning ?? msg;
            }
            else if (status == RenderStatus.Error)
            {
                MessageError = CustomMessageError ?? msg;
            }
            else if (status == RenderStatus.Content && (Equals(instance, default(T)) || ExpressionEmpty(instance)) && CustomMessageWarning.NotEmpty())
            {
                await ChangeStatus(RenderStatus.Warning, GlobalTranslations.CustomVisibilityNoData);
                return;
            }

            CurrentStatus = status;
            CurrentInstance = instance;

            OnStateChanged?.Invoke();
        }
    }
}
