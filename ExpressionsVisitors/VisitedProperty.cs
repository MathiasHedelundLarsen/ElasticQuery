namespace ElasticQuery.ExpressionsVisitors
{
    public class VisitedProperty
    {
        public string? Value { get; init; }
        public string? Alias { get; init; }

        public override string ToString()
        {
            return $"{Value} AS {Alias}";
        }
    }
}
