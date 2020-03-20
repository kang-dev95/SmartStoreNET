﻿using System;
using System.Collections.Generic;
using System.Linq;
using SmartStore.Core;
using SmartStore.Core.Domain.Catalog;
using SmartStore.Core.Localization;
using SmartStore.Core.Plugins;
using SmartStore.Core.Search;
using SmartStore.Rules;
using SmartStore.Rules.Domain;
using SmartStore.Rules.Filters;
using SmartStore.Services.Localization;
using SmartStore.Services.Search;

namespace SmartStore.Services.Catalog.Rules
{
    public class ProductRuleProvider : RuleProviderBase, IProductRuleProvider
    {
        private readonly IRuleFactory _ruleFactory;
        private readonly ICommonServices _services;
        private readonly ICatalogSearchService _catalogSearchService;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ICategoryService _categoryService;
        private readonly IPluginFinder _pluginFinder;
        private readonly CatalogSettings _catalogSettings;

        public ProductRuleProvider(
            IRuleFactory ruleFactory,
            ICommonServices services,
            ICatalogSearchService catalogSearchService,
            ISpecificationAttributeService specificationAttributeService,
            ICategoryService categoryService,
            IPluginFinder pluginFinder,
            CatalogSettings catalogSettings)
            : base(RuleScope.Product)
        {
            _ruleFactory = ruleFactory;
            _services = services;
            _catalogSearchService = catalogSearchService;
            _specificationAttributeService = specificationAttributeService;
            _categoryService = categoryService;
            _pluginFinder = pluginFinder;
            _catalogSettings = catalogSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public SearchFilterExpressionGroup CreateExpressionGroup(int ruleSetId)
        {
            return _ruleFactory.CreateExpressionGroup(ruleSetId, this) as SearchFilterExpressionGroup;
        }

        public override IRuleExpression VisitRule(RuleEntity rule)
        {
            var expression = new SearchFilterExpression();
            base.ConvertRule(rule, expression);
            expression.Descriptor = ((RuleExpression)expression).Descriptor as SearchFilterDescriptor;
            return expression;
        }

        public override IRuleExpressionGroup VisitRuleSet(RuleSetEntity ruleSet)
        {
            var group = new SearchFilterExpressionGroup()
            {
                Id = ruleSet.Id,
                LogicalOperator = ruleSet.LogicalOperator,
                IsSubGroup = ruleSet.IsSubGroup,
                Value = ruleSet.Id,
                RawValue = ruleSet.Id.ToString(),
                Provider = this
            };

            return group;
        }

        public IPagedList<Product> Search(SearchFilterExpression[] filters, int pageIndex = 0, int pageSize = int.MaxValue)
        {
            if ((filters?.Length ?? 0) == 0)
            {
                return new PagedList<Product>(Enumerable.Empty<Product>(), 0, int.MaxValue);
            }

            SearchFilterExpressionGroup group;

            if (filters.Length == 1 && filters[0] is SearchFilterExpressionGroup group2)
            {
                group = group2;
            }
            else
            {
                group = new SearchFilterExpressionGroup();
                group.AddExpressions(filters);
            }

            var searchQuery = new CatalogSearchQuery()
                .OriginatesFrom("Rule/Search")
                .WithLanguage(_services.WorkContext.WorkingLanguage)
                .WithCurrency(_services.WorkContext.WorkingCurrency)
                .BuildFacetMap(false)
                .CheckSpelling(0)
                .Slice(pageIndex * pageSize, pageSize)
                .SortBy(ProductSortingEnum.CreatedOn);

            searchQuery = group.ApplyFilters(searchQuery);

            var searchResult = _catalogSearchService.Search(searchQuery);

            return searchResult.Hits;
        }

        protected override IEnumerable<RuleDescriptor> LoadDescriptors()
        {
            var language = _services.WorkContext.WorkingLanguage;

            var stores = _services.StoreService.GetAllStores()
                .Select(x => new RuleValueSelectListOption { Value = x.Id.ToString(), Text = x.Name })
                .ToArray();

            var visibilities = ((ProductVisibility[])Enum.GetValues(typeof(ProductVisibility)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = x.GetLocalizedEnum(_services.Localization) })
                .ToArray();

            var productTypes = ((ProductType[])Enum.GetValues(typeof(ProductType)))
                .Select(x => new RuleValueSelectListOption { Value = ((int)x).ToString(), Text = x.GetLocalizedEnum(_services.Localization) })
                .ToArray();

            CatalogSearchQuery categoryFilter(CatalogSearchQuery q, int[] x)
            {
                if (x?.Any() ?? false)
                {
                    var ids = new HashSet<int>(x);

                    if (_catalogSettings.ShowProductsFromSubcategories)
                    {
                        var tree = _categoryService.GetCategoryTree(includeHidden: true);

                        foreach (var id in x)
                        {
                            var node = tree.SelectNodeById(id);
                            if (node != null)
                            {
                                ids.AddRange(node.Flatten(false).Select(x => x.Id));
                            }
                        }
                    }

                    q = q.WithCategoryIds(_catalogSettings.IncludeFeaturedProductsInNormalLists ? (bool?)null : false, ids.ToArray());
                }

                return q;
            }

            var descriptors = new List<SearchFilterDescriptor>
            {
                new SearchFilterDescriptor<int>((q, x) => q.HasStoreId(x))
                {
                    Name = "Store",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Store"),
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(stores),
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<int[]>((q, x) => q.AllowedCustomerRoles(x))
                {
                    Name = "CustomerRole",
                    DisplayName = T("Admin.Rules.FilterDescriptor.IsInCustomerRole"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("CustomerRole") { Multiple = true },
                    Operators = new RuleOperator[] { RuleOperator.In }
                },
                new SearchFilterDescriptor<bool>((q, x) => q.PublishedOnly(x))
                {
                    Name = "Published",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Published"),
                    RuleType = RuleType.Boolean,
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<bool>((q, x) => q.AvailableOnly(x))
                {
                    Name = "AvailableByStock",
                    DisplayName = T("Admin.Rules.FilterDescriptor.AvailableByStock"),
                    RuleType = RuleType.Boolean,
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<bool>((q, x) => q.AvailableByDate(x))
                {
                    Name = "AvailableByDate",
                    DisplayName = T("Admin.Rules.FilterDescriptor.AvailableByDate"),
                    RuleType = RuleType.Boolean,
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<int>((q, x) => q.WithVisibility((ProductVisibility)x))
                {
                    Name = "Visibility",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Visibility"),
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(visibilities),
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<int[]>((q, x) => q.WithProductIds(x))
                {
                    Name = "Product",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Product"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Product") { Multiple = true },
                    Operators = new RuleOperator[] { RuleOperator.In }
                },
                new SearchFilterDescriptor<bool>((q, x) => q.HomePageProductsOnly(x))
                {
                    Name = "HomepageProduct",
                    DisplayName = T("Admin.Rules.FilterDescriptor.HomepageProduct"),
                    RuleType = RuleType.Boolean,
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<int>((q, x) => q.IsProductType((ProductType)x))
                {
                    Name = "ProductType",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductType"),
                    RuleType = RuleType.Int,
                    SelectList = new LocalRuleValueSelectList(productTypes),
                    Operators = new RuleOperator[] { RuleOperator.IsEqualTo }
                },
                new SearchFilterDescriptor<int[]>(categoryFilter)
                {
                    Name = "Category",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Category"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Category") { Multiple = true },
                    Operators = new RuleOperator[] { RuleOperator.In }
                },
                new SearchFilterDescriptor<int[]>((q, x) => q.WithManufacturerIds(null, x))
                {
                    Name = "Manufacturer",
                    DisplayName = T("Admin.Rules.FilterDescriptor.Manufacturer"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("Manufacturer") { Multiple = true },
                    Operators = new RuleOperator[] { RuleOperator.In }
                },
                new SearchFilterDescriptor<int[]>((q, x) => q.WithProductTagIds(x))
                {
                    Name = "ProductTag",
                    DisplayName = T("Admin.Rules.FilterDescriptor.ProductTag"),
                    RuleType = RuleType.IntArray,
                    SelectList = new RemoteRuleValueSelectList("ProductTag") { Multiple = true },
                    Operators = new RuleOperator[] { RuleOperator.In }
                },
            };

            if (_pluginFinder.GetPluginDescriptorBySystemName("SmartStore.MegaSearchPlus") != null)
            {
                ISearchFilter[] optionFilters(int attrId, int[] optionIds)
                    => optionIds.Select(id => SearchFilter.ByField("attrvalueid", id).ExactMatch().NotAnalyzed().HasParent(attrId)).ToArray();

                var attributes = _specificationAttributeService.GetSpecificationAttributes()
                    .Where(x => x.AllowFiltering)
                    .ToList();

                foreach (var attr in attributes)
                {
                    var descriptor = new SearchFilterDescriptor<int[]>((q, x) => q.WithFilter(SearchFilter.Combined(optionFilters(attr.Id, x))))
                    {
                        Name = $"Attribute{attr.Id}",
                        DisplayName = attr.GetLocalized(x => x.Name, language, true, false),
                        GroupKey = "Admin.Catalog.Attributes.SpecificationAttributes",
                        RuleType = RuleType.IntArray,
                        SelectList = new RemoteRuleValueSelectList("AttributeOption") { Multiple = true },
                        Operators = new RuleOperator[] { RuleOperator.In }
                    };
                    descriptor.Metadata["ParentId"] = attr.Id;

                    descriptors.Add(descriptor);
                }
            }

            descriptors
                .Where(x => x.RuleType == RuleType.Money)
                .Each(x => x.Metadata["postfix"] = _services.StoreContext.CurrentStore.PrimaryStoreCurrency.CurrencyCode);

            return descriptors;
        }
    }
}
