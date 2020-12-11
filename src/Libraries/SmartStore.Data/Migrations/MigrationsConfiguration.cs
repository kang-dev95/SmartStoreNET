﻿namespace SmartStore.Data.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using Setup;
    using SmartStore.Core.Data;
    using SmartStore.Utilities;

    public sealed class MigrationsConfiguration : DbMigrationsConfiguration<SmartObjectContext>
    {
        public MigrationsConfiguration()
        {
            AutomaticMigrationsEnabled = false;
            AutomaticMigrationDataLossAllowed = true;
            ContextKey = "SmartStore.Core";

            if (DataSettings.Current.IsSqlServer)
            {
                var commandTimeout = CommonHelper.GetAppSetting<int?>("sm:EfMigrationsCommandTimeout");
                if (commandTimeout.HasValue)
                {
                    CommandTimeout = commandTimeout.Value;
                }

                CommandTimeout = 9999999;
            }
        }

        public void SeedDatabase(SmartObjectContext context)
        {
            using (var scope = new DbContextScope(context, hooksEnabled: false))
            {
                Seed(context);
                scope.Commit();
            }
        }

        protected override void Seed(SmartObjectContext context)
        {
            context.MigrateLocaleResources(MigrateLocaleResources);
            MigrateSettings(context);

            var logTypeMigrator = new ActivityLogTypeMigrator(context);
            logTypeMigrator.AddActivityLogType("EditOrder", "Edit an order", "Auftrag bearbeitet");
        }

        public void MigrateSettings(SmartObjectContext context)
        {

        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.CartItemQuantity", 
                "Product quantity is in range", 
                "Produktmenge liegt in folgendem Bereich");

            builder.AddOrUpdate("Newsletter.SubscriptionFailed",
                "The subscription or unsubscription has failed.",
                "Die Abonnierung bzw. Abbestellung ist fehlgeschlagen.");

            builder.AddOrUpdate("Common.UnsupportedBrowser",
                "You are using an unsupported browser! Please consider switching to a modern browser such as Google Chrome, Firefox or Opera to fully enjoy your shopping experience.",
                "Sie verwenden einen nicht unterstützten Browser! Bitte ziehen Sie in Betracht, zu einem modernen Browser wie Google Chrome, Firefox oder Opera zu wechseln, um Ihr Einkaufserlebnis in vollen Zügen genießen zu können.");

            builder.Delete("Admin.Configuration.Settings.Order.ApplyToSubtotal");
            builder.Delete("Checkout.MaxOrderTotalAmount");
            builder.Delete("Checkout.MinOrderTotalAmount");

            builder.AddOrUpdate("Checkout.MaxOrderSubtotalAmount",
                "Your maximum order total allowed is {0}.",
                "Ihr zulässiger Höchstbestellwert beträgt {0}.");

            builder.AddOrUpdate("Checkout.MinOrderSubtotalAmount",
                "Your minimum order total allowed is {0}.",
                "Ihr zulässiger Mindestbestellwert beträgt {0}.");

            builder.Delete("Admin.Configuration.Settings.Order.OrderTotalRestrictionType");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.MultipleOrderTotalRestrictionsExpandRange",
                "Customer groups extend the value range",
                "Kundengruppen erweitern den Wertebereich",
                "Specifies whether multiple order total restrictions through customer group assignments extend the allowed order value range.",
                "Legt fest, ob mehrfache Bestellwertbeschränkungen durch Kundengruppenzuordnungen den erlaubten Bestellwertbereich erweitern.");

            builder.AddOrUpdate("ActivityLog.EditOrder",
                "Edited order {0}",
                "Auftrag {0} bearbeitet");

            builder.AddOrUpdate("Admin.ContentManagement.Blog.BlogPosts.Fields.Language",
                "Regional relevance",
                "Regionale Relevanz",
                "Specifies the language for which the post is displayed. If limited to one language, blog contents need only be edited in that language (no multilingualism).",
                "Legt fest, für welche Sprache der Beitrag angezeigt wird. Bei einer Begrenzung auf eine Sprache brauchen Blog-Inhalte nur in dieser Sprache eingegeben zu werden (keine Mehrsprachigkeit).");

            builder.AddOrUpdate("Admin.ContentManagement.News.NewsItems.Fields.Language",
                "Regional relevance",
                "Regionale Relevanz",
                "Specifies the language for which the news is displayed. If limited to one language, news contents need only be edited in that language (no multilingualism).",
                "Legt fest, für welche Sprache die News angezeigt wird. Bei einer Begrenzung auf eine Sprache brauchen News-Inhalte nur in dieser Sprache eingegeben zu werden (keine Mehrsprachigkeit).");

            builder.AddOrUpdate("Common.International", "International", "International");

            builder.AddOrUpdate("Admin.Plugins.KnownGroup.B2B", "B2B", "B2B");
        }
    }
}
