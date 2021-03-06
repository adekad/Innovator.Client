/*
 * https://docs.microsoft.com/en-us/vsts/collaborate/wiql-syntax?view=vsts
 * https://msdn.microsoft.com/en-us/library/aa394054(v=vs.85).aspx
 * http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part2-url-conventions/odata-v4.0-errata03-os-part2-url-conventions-complete.html
 * https://developer.atlassian.com/server/confluence/advanced-searching-using-cql/
 * https://confluence.atlassian.com/jirasoftwarecloud/advanced-searching-764478330.html
 * https://docs.newrelic.com/docs/insights/nrql-new-relic-query-language/nrql-resources/nrql-syntax-components-functions
 * https://www.ibm.com/developerworks/community/wikis/form/anonymous/api/wiki/02db2a84-fc66-4667-b760-54e495526ec1/page/87348f89-b8b4-4c4a-94bd-ecbe1e4e8857/attachment/2f27f2b3-3583-4b3c-8ad1-ed35bb4e4279/media/MaximoNextGenRESTAPI%20%285%29.pdf
 * https://developers.google.com/chart/interactive/docs/querylanguage
 * 
 * https://restdb.io/docs/querying-with-the-api
 * https://tools.ietf.org/html/draft-nottingham-atompub-fiql-00
 * http://htsql.org/doc/overview.html
 *
 * https://lucene.apache.org/core/6_0_0/queryparser/org/apache/lucene/queryparser/classic/package-summary.html
 * https://docs.microsoft.com/en-us/rest/api/searchservice/lucene-query-syntax-in-azure-search
 * https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-query-string-query.html
 *
 * IDEAS:
 *    Add support for mapping the values in a query to another data model
 *    Create an "under" or "isa" operator for heirarchical fields
 *    
 *    IQueryable with Entity Framework: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/ef/language-reference/clr-method-to-canonical-function-mapping
 */

#if REFLECTION
using Innovator.Client.Queryable;
using System.Linq.Expressions;
#endif
using Innovator.Client.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace Innovator.Client.QueryModel
{
  /// <summary>
  /// Represents a query to retrieve information from a data store.  Can be converted to multiple
  /// formats such as SQL, OData, AML, etc.
  /// </summary>
  /// <seealso cref="Innovator.Client.IAmlNode" />
  [DebuggerDisplay("{DebuggerDisplay,nq}")]
  public class QueryItem : IAmlNode
  {
    private readonly List<Join> _joins = new List<Join>();
    private readonly List<OrderByExpression> _orderBy = new List<OrderByExpression>();
    private readonly List<SelectExpression> _select = new List<SelectExpression>();
    private readonly Dictionary<string, string> _attributes = new Dictionary<string, string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryItem"/> class.
    /// </summary>
    /// <param name="context">The context.</param>
    public QueryItem(IServerContext context)
    {
      Context = context;
    }

    /// <summary>
    /// Gets or sets the alias to use
    /// </summary>
    public string Alias { get; set; }
    /// <summary>
    /// Gets additional AML attributes.
    /// </summary>
    public IDictionary<string, string> Attributes { get { return _attributes; } }
    /// <summary>
    /// Gets the context for rendering native .Net types
    /// </summary>
    public IServerContext Context { get; }
    /// <summary>
    /// Gets or sets the name of the item type.
    /// </summary>
    public string Type { get; set; }
    /// <summary>
    /// Gets or sets the item property from which this <see cref="QueryItem"/> was generated
    /// </summary>
    public PropertyReference TypeProvider { get; set; }
    /// <summary>
    /// Gets or sets how many records to fetch.
    /// </summary>
    public int? Fetch { get; set; }
    /// <summary>
    /// Gets or sets how many records to skip.
    /// </summary>
    public int? Offset { get; set; }
    /// <summary>
    /// Gets the joined tables.
    /// </summary>
    public IList<Join> Joins { get { return _joins; } }
    /// <summary>
    /// Gets the expressions to order the results by.
    /// </summary>
    public IList<OrderByExpression> OrderBy { get { return _orderBy; } }
    /// <summary>
    /// Gets the select columns to return.
    /// </summary>
    public IList<SelectExpression> Select { get { return _select; } }
    /// <summary>
    /// Gets or sets the version.
    /// </summary>
    public IVersionCriteria Version { get; set; }
    /// <summary>
    /// Gets or sets the criteria used to filter the items to return.
    /// </summary>
    public IExpression Where { get; set; }

    private string DebuggerDisplay
    {
      get
      {
        using (var writer = new System.IO.StringWriter())
        {
          writer.Write("select {");
          writer.Write(Select.Count);
          writer.Write("} from ");
          if (string.IsNullOrEmpty(Type))
          {
            writer.Write("{");
            var visitor = new SqlServerVisitor(writer, new NullAmlSqlWriterSettings());
            TypeProvider.Visit(visitor);
            writer.Write("}");
          }
          else
          {
            writer.Write(Type);
          }
          if (!string.IsNullOrEmpty(Alias))
          {
            writer.Write(" as ");
            writer.Write(Alias);
          }
          if (Joins.Count > 0)
          {
            writer.Write(" join {");
            writer.Write(Joins.Count);
            writer.Write("}");
          }

          if (Where != null)
            writer.Write(" where ?");
          if (Offset > 0)
          {
            writer.Write(" offset ");
            writer.Write(Offset);
          }
          if (Fetch > 0)
          {
            writer.Write(" fetch ");
            writer.Write(Fetch);
          }

          writer.Flush();
          return writer.ToString();
        }
      }
    }

    /// <summary>
    /// Add a condition to the <see cref="Where"/> clause using an <see cref="AndOperator"/>.
    /// </summary>
    /// <param name="expr">Expression to add</param>
    public void AddCondition(IExpression expr)
    {
      if (Where == null)
      {
        Where = expr;
      }
      else
      {
        Where = new AndOperator()
        {
          Left = Where,
          Right = expr
        }.Normalize();
      }
    }

    /// <summary>
    /// Add a condition to the <see cref="Where"/> clause using an <see cref="AndOperator"/>.
    /// </summary>
    /// <param name="prop">Definition of the property</param>
    /// <param name="value">Serialized query to parse</param>
    /// <param name="parser">Parser settings to use</param>
    /// <param name="condition">The condition operate to use (if specified)</param>
    public void AddCondition(IPropertyDefinition prop, string value, SimpleSearchParser parser, Condition condition = Condition.Undefined)
    {
      AddCondition(parser.Parse(this, prop, value, condition));
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    public QueryItem Clone()
    {
      return new CloneVisitor().Clone(this);
    }

    /// <summary>
    /// Write the query to the specified <see cref="XmlWriter" /> as AML
    /// </summary>
    /// <param name="writer"><see cref="XmlWriter" /> to write the node to</param>
    /// <param name="settings">Settings controlling how the node is written</param>
    public void ToAml(XmlWriter writer, AmlWriterSettings settings)
    {
      var visitor = new AmlVisitor(Context, writer);
      visitor.Visit(this);
    }

    /// <summary>
    /// Write the query as SQL
    /// </summary>
    /// <param name="settings">The settings.</param>
    public string ToArasSql(IAmlSqlWriterSettings settings)
    {
      using (var writer = new StringWriter())
      {
        ToArasSql(writer, settings);
        writer.Flush();
        return writer.ToString();
      }
    }

    /// <summary>
    /// Write the query to the <paramref name="writer"/> as SQL
    /// </summary>
    /// <param name="writer">The writer</param>
    /// <param name="settings">The settings.</param>
    public void ToArasSql(TextWriter writer, IAmlSqlWriterSettings settings)
    {
      var visitor = new ArasSqlServerVisitor(writer, settings);
      var clone = new CloneVisitor().WithPropertyMapper(p =>
      {
        var table = p.Table;
        table.TryFillName(settings);
        if (string.IsNullOrEmpty(table.Type))
          return IgnoreNode.Instance;
        var props = settings.GetProperties(table.Type);
        if (props.Count < 1)
          return p;
        if (!props.TryGetValue(p.Name, out var propDefn))
          return IgnoreNode.Instance;
        if (propDefn.DataType().Value == "foreign")
          return table.GetProperty(propDefn);
        return p;
      }).Clone(this);
      visitor.Visit(clone);
    }

    /// <summary>
    /// Creates a list of advanced criteria to represent this query.
    /// </summary>
    /// <param name="parser">The parser.</param>
    public IEnumerable<Criteria> ToCriteria(SimpleSearchParser parser)
    {
      if (Where == null)
        return Enumerable.Empty<Criteria>();

      var visitor = new SimpleSearchVisitor(parser);
      Where.Visit(visitor);
      return visitor.Criteria;
    }

    /// <summary>
    /// Creates an OData URL string to represent the query.
    /// </summary>
    /// <param name="settings">The settings.</param>
    /// <param name="context">The context.</param>
    /// <param name="version">The version.</param>
    /// <returns>OData URL (relative to an unspecified base), e.g. Part?$skip=10&amp;$take=20</returns>
    public string ToOData(IQueryWriterSettings settings, IServerContext context, ODataVersion version = ODataVersion.All)
    {
      using (var writer = new StringWriter())
      {
        var visitor = new ODataVisitor(writer, settings, context, version);
        visitor.Visit(this);
        writer.Flush();
        return writer.ToString();
      }
    }

    internal PropertyReference GetProperty(IPropertyDefinition propDefn)
    {
      var defn = propDefn;
      var prop = new PropertyReference(defn.NameProp().Value
        ?? defn.KeyedName().Value
        ?? defn.IdProp().KeyedName().Value, this);

      while (defn.DataType().Value == "foreign"
        && defn.Property("foreign_property").HasValue()
        && defn.DataSource().KeyedName().HasValue())
      {
        var linkProp = new PropertyReference(defn.DataSource().KeyedName().Value, prop.Table);
        var table = linkProp.GetOrAddTable(Context);

        defn = (IPropertyDefinition)defn.Property("foreign_property").AsItem();
        if (string.IsNullOrEmpty(table.Type))
          table.Type = defn.SourceId().Attribute("name").Value ?? defn.SourceId().KeyedName().Value;
        prop = new PropertyReference(defn.NameProp().Value
          ?? defn.KeyedName().Value
          ?? defn.IdProp().KeyedName().Value, table);
      }

      return prop;
    }

    internal PropertyReference GetProperty(IEnumerable<string> path)
    {
      if (!path.Any())
        throw new ArgumentException();

      var table = this;
      var prop = default(PropertyReference);
      foreach (var segment in path)
      {
        if (prop != null)
          table = prop.GetOrAddTable(Context);
        prop = new PropertyReference(segment, table);
      }

      return prop;
    }

    internal void RebalanceCriteria()
    {
      if (!(Where is AndOperator and))
        return;

      var groups = BinaryOperator.Flatten(and)
        .GroupBy(g => (g as ITableProvider)?.Table ?? this)
        .ToArray();
      if (groups.Length == 1)
        return;

      Where = null;
      foreach (var group in groups)
      {
        foreach (var criteria in group)
        {
          group.Key.AddCondition(criteria);
        }
      }
    }

    internal void TryFillName(IQueryWriterSettings Settings)
    {
      if (string.IsNullOrEmpty(Type) && TypeProvider?.Table != null)
      {
        TypeProvider.Table.TryFillName(Settings);
        if (!string.IsNullOrEmpty(TypeProvider.Table.Type))
        {
          var props = Settings.GetProperties(TypeProvider.Table.Type);
          if (props != null
            && !string.IsNullOrEmpty(TypeProvider.Name)
            && props.TryGetValue(TypeProvider.Name, out var propDefn)
            && propDefn.DataType().Value == "item")
          {
            Type = propDefn.DataSource().KeyedName().Value;
          }
        }
      }
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="xml">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    public static QueryItem FromXml(Stream xml, IServerContext context = null)
    {
      var xmlStream = xml as IXmlStream;
      using (var xmlReader = (xmlStream == null ? XmlReader.Create(xml) : xmlStream.CreateReader()))
      {
        return FromXml(xmlReader, context);
      }
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="xml">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    public static QueryItem FromXml(TextReader xml, IServerContext context = null)
    {
      using (var xmlReader = XmlReader.Create(xml))
      {
        return FromXml(xmlReader, context);
      }
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="xml">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    /// <param name="args">Arguments to substitute into the query</param>
    public static QueryItem FromXml(string xml, IServerContext context, params object[] args)
    {
      return FromXml(w =>
      {
        var sub = new ParameterSubstitution();
        sub.AddIndexedParameters(args);
        sub.Substitute(xml, context, w);
      }, context);
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="xml">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    public static QueryItem FromXml(XmlReader xml, IServerContext context = null)
    {
      return FromXml(w => xml.CopyTo(w), context);
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="node">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    public static QueryItem FromXml(IAmlNode node, IServerContext context = null)
    {
      context = context
        ?? (node as IReadOnlyElement)?.AmlContext.LocalizationContext
        ?? ElementFactory.Local.LocalizationContext;
      return FromXml(w => node.ToAml(w, new AmlWriterSettings()), context);
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="cmd">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    public static QueryItem FromXml(Command cmd, IServerContext context = null)
    {
      context = context ?? ElementFactory.Local.LocalizationContext;
      return FromXml(w => cmd.ToNormalizedAml(context, w), context);
    }

    /// <summary>
    /// Converts an AML node into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="writer">XML data</param>
    /// <param name="context">Localization context for parsing and formating data</param>
    public static QueryItem FromXml(Action<XmlWriter> writer, IServerContext context = null)
    {
      context = context ?? ElementFactory.Local.LocalizationContext;
      using (var w = new AnyAmlWriter(context))
      {
        writer(w);
        return w.Query;
      }
    }

    /// <summary>
    /// Converts an OData URL into a query which can be converted to other forms (e.g. SQL, OData, ...)
    /// </summary>
    /// <param name="url">The OData URL.</param>
    /// <param name="context">The context.</param>
    /// <param name="version">The version.</param>
    public static QueryItem FromOData(string url, IServerContext context = null, ODataVersion version = ODataVersion.All)
    {
      context = context ?? ElementFactory.Local.LocalizationContext;
      return ODataParser.Parse(url, context, version);
    }

#if REFLECTION
    public static QueryItem FromLinq(string itemType, Func<IOrderedQueryable<IReadOnlyItem>, object> writer, IServerContext context = null)
    {
      return FromLinq<IReadOnlyItem>(itemType, writer, context);
    }

    public static QueryItem FromLinq<T>(string itemType, Func<IOrderedQueryable<T>, object> writer, IServerContext context = null) where T : IReadOnlyItem
    {
      var factory = ElementFactory.Local;
      if (context != null)
        factory = new ElementFactory(context);
      var query = default(AmlQuery);

      var queryable = new InnovatorQuery<T>(new InnovatorQueryProvider(factory, q => query = q), itemType);
      var resultQuery = writer(queryable) as IQueryable;
      if (resultQuery != null && resultQuery.Provider is InnovatorQueryProvider provider)
        query = provider.Translate(resultQuery.Expression);

      return query.QueryItem;
    }
#endif
  }
}
