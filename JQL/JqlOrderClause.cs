using PowNet.Common;

namespace JQL
{
    public class JqlOrderClause(string name, OrderDirection orderDirection = OrderDirection.ASC)
	{
		public string Name { set; get; } = name;
		public OrderDirection OrderDirection { set; get; } = orderDirection;
	}
}
