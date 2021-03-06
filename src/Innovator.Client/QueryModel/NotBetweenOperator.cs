using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Innovator.Client.QueryModel
{
  internal class NotBetweenOperator : BetweenOperator
  {
    public override IExpression Normalize()
    {
      return new NotOperator()
      {
        Arg = new BetweenOperator()
        {
          Left = Left,
          Min = Min,
          Max = Max
        }.Normalize()
      }.Normalize();
    }

    public override void Visit(IExpressionVisitor visitor)
    {
      throw new NotSupportedException();
    }
  }
}
