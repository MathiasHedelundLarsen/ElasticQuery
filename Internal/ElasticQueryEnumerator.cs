using System.Collections;
using System.Linq.Expressions;

namespace ElasticQuery.Internal
{
    internal sealed class ElasticQueryEnumerator<T> : IEnumerator<T>, IAsyncEnumerator<T>
    {
        private IEnumerator<T>? _innerEnumerator;
        private readonly Expression _queryExpression;
        private readonly CancellationToken _cancellationToken;
        private readonly ElasticQueryProvider _elasticQueryProvider;

        public T Current => _innerEnumerator!.Current;

#pragma warning disable CS8603 // Possible null reference return.
        object IEnumerator.Current => Current;
#pragma warning restore CS8603 // Possible null reference return.

        public ElasticQueryEnumerator(ElasticQueryProvider elasticQueryProvider, Expression queryExpression, CancellationToken cancellationToken = default)
        {
            _elasticQueryProvider = elasticQueryProvider;
            _queryExpression = queryExpression;
            _cancellationToken = cancellationToken;
        }

        public void Dispose()
        {
            _innerEnumerator?.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask; 
        }

        public bool MoveNext()
        {
            if (_innerEnumerator == null)
            {
                _innerEnumerator = _elasticQueryProvider.Execute<List<T>>(_queryExpression).GetEnumerator();
            }

            return _innerEnumerator.MoveNext();
        }

        public async ValueTask<bool> MoveNextAsync()
        {
            if (_innerEnumerator == null)
            {
                _innerEnumerator = (await _elasticQueryProvider.ExecuteAsync<List<T>>(_queryExpression, _cancellationToken)).GetEnumerator();
            }

            return _innerEnumerator.MoveNext();
        }

        public void Reset()
        {
            _innerEnumerator = null;
        }
    }
}
