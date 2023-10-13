using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace DotNetCoreSqlDb.Controllers.Constraints
{
    public class ContainsHeader : Attribute, IActionConstraint
    {
        private readonly string _value;
        private readonly string _header;

        public ContainsHeader(string header, string value)
        {
            _header = header;
            _value = value;
        }

        public bool Accept(ActionConstraintContext context)
        {
            if (!context.RouteContext.HttpContext.Request.Headers.ContainsKey(_header))
            {
                return false;
            }

            var headerValue = context.RouteContext.HttpContext.Request.Headers[_header];
            return headerValue.Contains(_value);
        }

        public int Order => 0;
    }
}
