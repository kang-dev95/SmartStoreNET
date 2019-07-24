﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmartStore.Rules;

namespace SmartStore.Services.Cart.Rules.Impl
{
    public class RuleRule : IRule
    {
        private readonly IRuleFactory _ruleFactory;
        private readonly ICartRuleProvider _cartRuleProvider;

        public RuleRule(IRuleFactory ruleFactory, ICartRuleProvider cartRuleProvider)
        {
            _ruleFactory = ruleFactory;
            _cartRuleProvider = cartRuleProvider;
        }

        protected RuleExpression GetOtherExpression(RuleExpression expression)
        {
            var ruleSetId = expression.Value.Convert<int>();
            var otherExpression = _ruleFactory.CreateExpressionGroup(ruleSetId, _cartRuleProvider) as RuleExpression;
            return otherExpression;
        }

        public bool Match(CartRuleContext context, RuleExpression expression)
        {
            var otherExpression = GetOtherExpression(expression);
            if (otherExpression == null)
                return false; // TBD: really?!

            var otherRule = _cartRuleProvider.GetProcessor(otherExpression);
            var otherMatch = otherRule.Match(context, otherExpression);

            //return expression.Operator.Match(otherMatch, true);

            if (expression.Operator == RuleOperator.IsEqualTo)
            {
                return otherRule.Match(context, otherExpression);
            }
            if (expression.Operator == RuleOperator.IsNotEqualTo)
            {
                return !otherRule.Match(context, otherExpression);
            }

            throw new InvalidRuleOperatorException(expression);
        }
    }
}
