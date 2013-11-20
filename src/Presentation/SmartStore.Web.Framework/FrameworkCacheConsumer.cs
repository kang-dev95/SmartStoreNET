﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SmartStore.Core.Caching;
using SmartStore.Core.Domain.Configuration;
using SmartStore.Core.Domain.Customers;
using SmartStore.Core.Domain.Localization;
using SmartStore.Core.Domain.Stores;
using SmartStore.Core.Domain.Themes;
using SmartStore.Core.Events;
using SmartStore.Core.Infrastructure;
using SmartStore.Core.Themes;
using SmartStore.Services.Events;

// codehint: sm-add (whole file)

namespace SmartStore.Web.Framework
{

    public class FrameworkCacheConsumer :
        IConsumer<EntityInserted<ThemeVariable>>,
        IConsumer<EntityUpdated<ThemeVariable>>,
        IConsumer<EntityDeleted<ThemeVariable>>,
        IConsumer<EntityInserted<CustomerRole>>,
        IConsumer<EntityUpdated<CustomerRole>>,
        IConsumer<EntityDeleted<CustomerRole>>,
        IConsumer<EntityInserted<Store>>,
        IConsumer<EntityDeleted<Store>>,
        IConsumer<EntityInserted<Language>>,
        IConsumer<EntityUpdated<Language>>,
        IConsumer<EntityDeleted<Language>>,
        IConsumer<EntityUpdated<Setting>>
    {
        /// <summary>
        /// Key for ThemeVariables caching
        /// </summary>
        /// <remarks>
        /// {0} : current theme name
        /// {1} : store identifier
        /// </remarks>
        public const string THEMEVARS_RAW_KEY = "sm.pres.themevars-raw-{0}-{1}";
        public const string THEMEVARS_LESSCSS_KEY = "sm.pres.themevars-lesscss-{0}-{1}";
        public const string THEMEVARS_PATTERN_KEY = "sm.pres.themevars";

        /// <summary>
        /// Key for tax display type caching
        /// </summary>
        /// <remarks>
        /// {0} : customer role ids
        /// {1} : store identifier
        /// </remarks>
        public const string CUSTOMERROLES_TAX_DISPLAY_TYPES_KEY = "sm.fw.customerroles.taxdisplaytypes-{0}-{1}";
        public const string CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY = "sm.fw.customerroles.taxdisplaytypes";

        public const string STORE_LANGUAGE_MAP_KEY = "sm.fw.storelangmap";

        private readonly ICacheManager _cacheManager;

        public FrameworkCacheConsumer()
        {
            // TODO inject static cache manager using constructor
            this._cacheManager = EngineContext.Current.ContainerManager.Resolve<ICacheManager>("sm_cache_static");
        }

        public void HandleEvent(EntityInserted<ThemeVariable> eventMessage)
        {
            // remove cached vars for modified theme
            _cacheManager.Remove(THEMEVARS_RAW_KEY.FormatInvariant(eventMessage.Entity.Theme, eventMessage.Entity.StoreId));
            _cacheManager.Remove(THEMEVARS_LESSCSS_KEY.FormatInvariant(eventMessage.Entity.Theme, eventMessage.Entity.StoreId));
        }

        public void HandleEvent(EntityUpdated<ThemeVariable> eventMessage)
        {
            // remove cached vars for modified theme
            _cacheManager.Remove(THEMEVARS_RAW_KEY.FormatInvariant(eventMessage.Entity.Theme, eventMessage.Entity.StoreId));
            _cacheManager.Remove(THEMEVARS_LESSCSS_KEY.FormatInvariant(eventMessage.Entity.Theme, eventMessage.Entity.StoreId));
        }

        public void HandleEvent(EntityDeleted<ThemeVariable> eventMessage)
        {
            // remove cached vars for modified theme
            _cacheManager.Remove(THEMEVARS_RAW_KEY.FormatInvariant(eventMessage.Entity.Theme, eventMessage.Entity.StoreId));
            _cacheManager.Remove(THEMEVARS_LESSCSS_KEY.FormatInvariant(eventMessage.Entity.Theme, eventMessage.Entity.StoreId));
        }


        public void HandleEvent(EntityDeleted<CustomerRole> eventMessage)
        {
            _cacheManager.RemoveByPattern(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY);
        }

        public void HandleEvent(EntityUpdated<CustomerRole> eventMessage)
        {
            _cacheManager.RemoveByPattern(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY);
        }

        public void HandleEvent(EntityInserted<CustomerRole> eventMessage)
        {
            _cacheManager.RemoveByPattern(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY);
        }


        public void HandleEvent(EntityInserted<Store> eventMessage)
        {
            _cacheManager.Remove(STORE_LANGUAGE_MAP_KEY);
        }

        public void HandleEvent(EntityDeleted<Store> eventMessage)
        {
            _cacheManager.Remove(STORE_LANGUAGE_MAP_KEY);
        }

        public void HandleEvent(EntityInserted<Language> eventMessage)
        {
            _cacheManager.Remove(STORE_LANGUAGE_MAP_KEY);
        }

        public void HandleEvent(EntityUpdated<Language> eventMessage)
        {
            _cacheManager.Remove(STORE_LANGUAGE_MAP_KEY);
        }

        public void HandleEvent(EntityDeleted<Language> eventMessage)
        {
            _cacheManager.Remove(STORE_LANGUAGE_MAP_KEY);
        }

        public void HandleEvent(EntityUpdated<Setting> eventMessage)
        {
            // clear models which depend on settings
            _cacheManager.RemoveByPattern(CUSTOMERROLES_TAX_DISPLAY_TYPES_PATTERN_KEY); // depends on TaxSettings.TaxDisplayType
        }
    }

}
