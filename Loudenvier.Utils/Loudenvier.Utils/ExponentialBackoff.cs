using System;
using System.Threading.Tasks;

namespace Loudenvier.Utils
{
    public abstract class Backoff
    {
        static TimeSpan DefaultMaxDelay { get; } = TimeSpan.FromSeconds(30);

        public Backoff(TimeSpan baseDelay, TimeSpan? maxDelay = null, int maxAttempts = 3) {
            BaseDelay = baseDelay;
            MaxDelay = maxDelay ?? DefaultMaxDelay;
            MaxAttempts = maxAttempts;
        }

        public TimeSpan BaseDelay { get; }
        public TimeSpan MaxDelay { get; }
        public int MaxAttempts { get; }
        public int Attempts { get; protected set; }
        public bool Expired => Attempts >= MaxAttempts;
        
        public void Reset() => Attempts = 0;
        
        public bool Delay() => DelayAsync().GetAwaiter().GetResult();   
        public async Task<bool> DelayAsync() {
            if (Expired) return false;
            await GetDelay().ConfigureAwait(false);
            Attempts++;
            return true;
        }
        protected abstract Task GetDelay();

        #region Statics which determine the delay time for backoff strategies (you can use them directly to do your own delaying)

        static void ValidateDelays(TimeSpan baseDelay, TimeSpan maxDelay) {
            if (baseDelay <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(baseDelay), baseDelay, "should be greater than TimeSpan.Zero (0).");
            if (maxDelay <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxDelay), maxDelay, "should be greater than TimeSpan.Zero (0).");
        }
        public static TimeSpan Exponential(ref int attempt, TimeSpan baseDelay, TimeSpan? maxDelay = null) {
            maxDelay ??= DefaultMaxDelay;
            ValidateDelays(baseDelay, maxDelay.Value);
            var delay = TimeSpan.FromTicks((2 << attempt++) * baseDelay.Ticks);
            return Time.Min(delay, maxDelay); 
        }
        public static TimeSpan Exponential(int attempt, TimeSpan baseDelay, TimeSpan? maxDelay = null) =>
            Exponential(ref attempt, baseDelay, maxDelay);

        public static TimeSpan Linear(ref int attempt, TimeSpan baseDelay, TimeSpan? maxDelay = null, double factor = 2) {
            maxDelay ??= DefaultMaxDelay;
            ValidateDelays(baseDelay, maxDelay.Value);
            var delay = TimeSpan.FromTicks((long)(attempt * factor * baseDelay.Ticks));
            return Time.Min(delay, maxDelay);
        }
        public static TimeSpan Linear(int attempt, TimeSpan baseDelay, TimeSpan? maxDelay = null, double factor = 2) =>
            Linear(ref attempt, baseDelay, maxDelay, factor);

        #endregion
    }

    public class LinearBackoff : Backoff {
        public LinearBackoff(TimeSpan baseDelay, TimeSpan? maxDelay = null, int maxAttempts = 3, double factor = 2.0) 
            : base(baseDelay, maxDelay, maxAttempts) {
            Factor = factor;
        }
        public double Factor { get; }
        protected override Task GetDelay() => Task.Delay(Linear(Attempts, BaseDelay, MaxDelay, Factor));
    }

    public class ExponentialBackoff : Backoff
    {
        public ExponentialBackoff(TimeSpan baseDelay, TimeSpan? maxDelay = null, int maxAttempts = 3)
            : base(baseDelay, maxDelay, maxAttempts) { }
        protected override Task GetDelay() => Task.Delay(Exponential(Attempts, BaseDelay, MaxDelay));
    }

    /*public enum BackoffStrategy { Exponential, Linear, Constant }
      public override async Task<bool> Delay() {
            if (Expired) return false;
            var attempts = Attempts;
            var delay = Strategy switch {
                BackoffStrategy.Linear => Linear(ref attempts, BaseDelay, MaxDelay),
                BackoffStrategy.Exponential => Exponential(ref attempts, BaseDelay, MaxDelay),
                BackoffStrategy.Constant => Task.Delay(BaseDelay),
                _ => throw new ArgumentOutOfRangeException(nameof(Strategy), Strategy, "Invalig backoff strategy value."),
            };
            Attempts = attempts;
            await delay.ConfigureAwait(false);
            return true;
        }
*/
}
