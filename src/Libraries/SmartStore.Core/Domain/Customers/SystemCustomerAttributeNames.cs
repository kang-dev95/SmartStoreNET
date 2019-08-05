
namespace SmartStore.Core.Domain.Customers
{
    public static partial class SystemCustomerAttributeNames
    {
        // Form fields
        public static string StreetAddress { get { return "StreetAddress"; } }
        public static string StreetAddress2 { get { return "StreetAddress2"; } }
        public static string City { get { return "City"; } }
        public static string StateProvinceId { get { return "StateProvinceId"; } }
        public static string Phone { get { return "Phone"; } }
        public static string Fax { get { return "Fax"; } }
		public static string VatNumber { get { return "VatNumber"; } }

        // Other attributes
		public static string DiscountCouponCode { get { return "DiscountCouponCode"; } }
		public static string GiftCardCouponCodes { get { return "GiftCardCouponCodes"; } }
		public static string CheckoutAttributes { get { return "CheckoutAttributes"; } }
        public static string AvatarPictureId { get { return "AvatarPictureId"; } }
        public static string AvatarColor { get { return "AvatarColor"; } }
        public static string ForumPostCount { get { return "ForumPostCount"; } }
        public static string Signature { get { return "Signature"; } }
        public static string PasswordRecoveryToken { get { return "PasswordRecoveryToken"; } }
        public static string AccountActivationToken { get { return "AccountActivationToken"; } }
        public static string LastVisitedPage { get { return "LastVisitedPage"; } }
		public static string ImpersonatedCustomerId { get { return "ImpersonatedCustomerId"; } }
		public static string AdminAreaStoreScopeConfiguration { get { return "AdminAreaStoreScopeConfiguration"; } }
		public static string MostRecentlyUsedCategories { get { return "MostRecentlyUsedCategories"; } }
		public static string MostRecentlyUsedManufacturers { get { return "MostRecentlyUsedManufacturers"; } }
		public static string WalletEnabled { get { return "WalletEnabled"; } }
		public static string HasConsentedToGdpr { get { return "HasConsentedToGdpr"; } }

		// Depends on store
		public static string SelectedPaymentMethod { get { return "SelectedPaymentMethod"; } }
		public static string SelectedShippingOption { get { return "SelectedShippingOption"; } }
		public static string OfferedShippingOptions { get { return "OfferedShippingOptions"; } }
		public static string LastContinueShoppingPage { get { return "LastContinueShoppingPage"; } }
		public static string NotifiedAboutNewPrivateMessages { get { return "NotifiedAboutNewPrivateMessages"; } }
		public static string WorkingThemeName { get { return "WorkingThemeName"; } }
		public static string UseRewardPointsDuringCheckout { get { return "UseRewardPointsDuringCheckout"; } }
		public static string UseCreditBalanceDuringCheckout { get { return "UseCreditBalanceDuringCheckout"; } }
	}
}