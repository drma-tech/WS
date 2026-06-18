using System.Diagnostics;
using System.Text.Json;

namespace WS.Shared.Core.Helper
{
    public sealed class OperationTrace
    {
        private readonly Stopwatch _total = Stopwatch.StartNew();
        private readonly List<ProfileStep> _steps = [];

        public IDisposable Measure(string name)
        {
            return new StepTimer(name, _steps);
        }

        public override string ToString()
        {
            return JsonSerializer.Serialize(GetResultObj());
        }

        private object GetResultObj()
        {
            _total.Stop();

            return new
            {
                TotalMs = _total.ElapsedMilliseconds,
                Steps = _steps
            };
        }

        private sealed class StepTimer(string name, List<ProfileStep> steps) : IDisposable
        {
            private readonly Stopwatch _sw = Stopwatch.StartNew();

            public void Dispose()
            {
                _sw.Stop();

                steps.Add(new ProfileStep
                {
                    Name = name,
                    ElapsedMs = _sw.ElapsedMilliseconds
                });
            }
        }

        private sealed class ProfileStep
        {
            public string? Name { get; init; }
            public long ElapsedMs { get; init; }
        }
    }
}