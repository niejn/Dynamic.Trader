using System;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using TradeExample.Annotations;
using Trader.Domain.Infrastucture;

namespace Trader.Domain.Model
{
    public class TradesByPercentDiff: IDisposable, IEquatable<TradesByPercentDiff>
    {
        private readonly IGroup<Trade, long, int> _group;
        private readonly IObservableCollection<TradeProxy> _data= new ObservableCollectionExtended<TradeProxy>();
        private readonly IDisposable _cleanUp;

        public TradesByPercentDiff([NotNull] IGroup<Trade, long, int> @group, [NotNull] ISchedulerProvider schedulerProvider, ILogger logger)
        {
            if (@group == null) throw new ArgumentNullException(nameof(@group));
            if (schedulerProvider == null) throw new ArgumentNullException(nameof(schedulerProvider));

            _group = @group;
            PercentBand = @group.Key;
           
            _cleanUp = @group.Cache.Connect()
                        .Transform(trade => new TradeProxy(trade))
                        .Batch(TimeSpan.FromMilliseconds(250))
                        .Sort(SortExpressionComparer<TradeProxy>.Descending(p => p.Timestamp), SortOptimisations.ComparesImmutableValuesOnly)
                        .ObserveOn(schedulerProvider.MainThread)
                        .Bind(_data)
                        .DisposeMany()
                        .Subscribe(_ => { }, ex => logger.Error(ex, "Error in TradesByPercentDiff"));
        }

        public int PercentBand { get; }

        private int PercentBandUpperBound => _group.Key + 1;

        public IObservableCollection<TradeProxy> Data => _data;

        public void Dispose()
        {
            _cleanUp.Dispose();
        }

        #region Equality

        public bool Equals(TradesByPercentDiff other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return PercentBand == other.PercentBand;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TradesByPercentDiff) obj);
        }

        public override int GetHashCode()
        {
            return PercentBand;
        }

        public static bool operator ==(TradesByPercentDiff left, TradesByPercentDiff right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TradesByPercentDiff left, TradesByPercentDiff right)
        {
            return !Equals(left, right);
        }

        #endregion

    }
}