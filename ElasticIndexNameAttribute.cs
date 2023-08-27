namespace ElasticQuery
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ElasticIndexNameAttribute : Attribute
    {        
        public string Name { get; }

        public ElasticIndexNameAttribute(string name)
        {
            Name = name;
        }
    }
}
