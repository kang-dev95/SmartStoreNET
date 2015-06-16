﻿using System.Data.Entity.ModelConfiguration;
using SmartStore.Core.Domain.Payments;

namespace SmartStore.Data.Mapping.Payments
{
	public partial class PaymentMethodMap : EntityTypeConfiguration<PaymentMethod>
	{
		public PaymentMethodMap()
		{
			this.ToTable("PaymentMethod");
			this.HasKey(x => x.Id);

			this.Property(x => x.PaymentMethodSystemName).IsRequired().HasMaxLength(4000);

			this.Property(x => x.ExcludedCustomerRoleIds).IsMaxLength();
			this.Property(x => x.ExcludedCountryIds).IsMaxLength();
			this.Property(x => x.ExcludedShippingMethodIds).IsMaxLength();

			this.Ignore(x => x.CountryExclusionContext);
		}
	}
}
