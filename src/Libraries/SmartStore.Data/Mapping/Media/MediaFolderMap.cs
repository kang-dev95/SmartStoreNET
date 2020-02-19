﻿using System;
using System.Data.Entity.ModelConfiguration;
using SmartStore.Core.Domain.Media;

namespace SmartStore.Data.Mapping.Media
{
    public partial class MediaFolderMap : EntityTypeConfiguration<MediaFolder>
    {
        public MediaFolderMap()
        {
            ToTable("MediaFolder");
            HasKey(c => c.Id);
            Property(c => c.Name).IsRequired().HasMaxLength(100);
            Property(c => c.Slug).HasMaxLength(100);
            Property(c => c.Metadata).IsMaxLength();

            HasOptional(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .WillCascadeOnDelete(false);
        }
    }

    public partial class MediaRegionMap : EntityTypeConfiguration<MediaRegion>
    {
        public MediaRegionMap()
        {
            Property(c => c.ResKey).HasMaxLength(255);
            Property(c => c.Icon).HasMaxLength(100);
            Property(c => c.Color).HasMaxLength(100);
        }
    }
}
