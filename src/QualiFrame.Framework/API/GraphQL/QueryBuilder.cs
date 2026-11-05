using System.Collections.Generic;
using System.Text;

namespace QualiFrame.Framework.API.GraphQL
{
    /// <summary>
    /// Builder for constructing GraphQL queries.
    /// </summary>
    public class QueryBuilder
    {
        private readonly StringBuilder _query;
        private readonly List<string> _fields;
        private string? _operationName;
        private Dictionary<string, object>? _variables;

        public QueryBuilder()
        {
            _query = new StringBuilder();
            _fields = new List<string>();
        }

        /// <summary>
        /// Sets the operation name.
        /// </summary>
        public QueryBuilder Operation(string operationName)
        {
            _operationName = operationName;
            return this;
        }

        /// <summary>
        /// Adds a field to the query.
        /// </summary>
        public QueryBuilder Field(string fieldName)
        {
            _fields.Add(fieldName);
            return this;
        }

        /// <summary>
        /// Adds multiple fields to the query.
        /// </summary>
        public QueryBuilder Fields(params string[] fieldNames)
        {
            _fields.AddRange(fieldNames);
            return this;
        }

        /// <summary>
        /// Adds a nested field with sub-fields.
        /// </summary>
        public QueryBuilder NestedField(string fieldName, params string[] subFields)
        {
            var nested = new StringBuilder();
            nested.Append($"{fieldName} {{ ");
            nested.Append(string.Join(" ", subFields));
            nested.Append(" }");
            _fields.Add(nested.ToString());
            return this;
        }

        /// <summary>
        /// Adds variables to the query.
        /// </summary>
        public QueryBuilder Variables(Dictionary<string, object> variables)
        {
            _variables = variables;
            return this;
        }

        /// <summary>
        /// Builds the GraphQL query string.
        /// </summary>
        public string Build()
        {
            var queryBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(_operationName))
            {
                queryBuilder.Append($"query {_operationName}");
            }
            else
            {
                queryBuilder.Append("query");
            }

            if (_variables != null && _variables.Count > 0)
            {
                queryBuilder.Append("(");
                var varList = new List<string>();
                foreach (var variable in _variables)
                {
                    var type = GetGraphQLType(variable.Value);
                    varList.Add($"${variable.Key}: {type}");
                }
                queryBuilder.Append(string.Join(", ", varList));
                queryBuilder.Append(")");
            }

            queryBuilder.Append(" { ");

            if (_fields.Count > 0)
            {
                queryBuilder.Append(string.Join(" ", _fields));
            }

            queryBuilder.Append(" }");

            return queryBuilder.ToString();
        }

        /// <summary>
        /// Builds a mutation query.
        /// </summary>
        public string BuildMutation()
        {
            var queryBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(_operationName))
            {
                queryBuilder.Append($"mutation {_operationName}");
            }
            else
            {
                queryBuilder.Append("mutation");
            }

            if (_variables != null && _variables.Count > 0)
            {
                queryBuilder.Append("(");
                var varList = new List<string>();
                foreach (var variable in _variables)
                {
                    var type = GetGraphQLType(variable.Value);
                    varList.Add($"${variable.Key}: {type}");
                }
                queryBuilder.Append(string.Join(", ", varList));
                queryBuilder.Append(")");
            }

            queryBuilder.Append(" { ");

            if (_fields.Count > 0)
            {
                queryBuilder.Append(string.Join(" ", _fields));
            }

            queryBuilder.Append(" }");

            return queryBuilder.ToString();
        }

        private string GetGraphQLType(object value)
        {
            return value switch
            {
                int => "Int",
                long => "Int",
                float => "Float",
                double => "Float",
                bool => "Boolean",
                string => "String",
                _ => "String"
            };
        }
    }
}
