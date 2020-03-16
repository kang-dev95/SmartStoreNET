﻿using System;
using System.Collections.Generic;
using FluentValidation;
using FluentValidation.Attributes;
using SmartStore.Rules;
using SmartStore.Web.Framework;
using SmartStore.Web.Framework.Modelling;

namespace SmartStore.Admin.Models.Rules
{
    [Validator(typeof(RuleSetValidator))]
    public class RuleSetModel : EntityModelBase
    {
        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.Name")]
        public string Name { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.Description")]
        public string Description { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.IsActive")]
        public bool IsActive { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.Scope")]
        public RuleScope Scope { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.Scope")]
        public string ScopeName { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.IsSubGroup")]
        public bool IsSubGroup { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.LogicalOperator")]
        public LogicalRuleOperator LogicalOperator { get; set; }

        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public DateTime? LastProcessedOnUtc { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.AssignedToDiscounts")]
        public IList<AssignedToEntityModel> AssignedToDiscounts { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.AssignedToShippingMethods")]
        public IList<AssignedToEntityModel> AssignedToShippingMethods { get; set; }

        [SmartResourceDisplayName("Admin.Rules.RuleSet.Fields.AssignedToPaymentMethods")]
        public IList<AssignedToEntityModel> AssignedToPaymentMethods { get; set; }

        public IRuleExpressionGroup ExpressionGroup { get; set; }
        public IEnumerable<RuleDescriptor> AvailableDescriptors { get; set; }

        public string RawRuleData { get; set; }

        public class AssignedToEntityModel : EntityModelBase
        {
            public string Name { get; set; }
            public string SystemName { get; set; }
        }
    }


    public class RuleSetPreviewModel : RuleSetModel
    {
        public int GridPageSize { get; set; }
        public bool UsernamesEnabled { get; set; }
    }


    public partial class RuleSetValidator : AbstractValidator<RuleSetModel>
    {
        public RuleSetValidator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Description).MaximumLength(400);
        }
    }
}