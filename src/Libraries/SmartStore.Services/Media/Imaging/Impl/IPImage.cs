﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageProcessor;

namespace SmartStore.Services.Media.Impl
{
    internal class IPImage : DisposableObject, IImage
    {        
        private readonly ImageFactory _image;
        
        public IPImage(ImageFactory image)
        {
            _image = image;
            SourceSize = new Size(image.Image.Width, image.Image.Height);
        }

        #region IImageInfo

        /// <inheritdoc/>
        public Size Size => _image.Image.Size;

        /// <inheritdoc/>
        public BitDepth BitDepth 
        {
            get => (BitDepth)Convert.ToInt32(_image.CurrentBitDepth);
            set => _image.BitDepth(Convert.ToInt64(value));
        }

        /// <inheritdoc/>
        public IImageFormat Format
        {
            get => new IPImageFormat(_image.CurrentImageFormat);
            set
            {
                Guard.NotNull(value, nameof(value));
                _image.Format(((IPImageFormat)value).WrappedFormat);
            }
        }

        #endregion

        /// <inheritdoc/>
        public Size SourceSize { get; }

        /// <inheritdoc/>
        public IImage Transform(Action<IImageTransformer> transformer)
        {
            transformer(new IPImageTransformer(_image));
            return this;
        }

        /// <inheritdoc/>
        public IImage Save(Stream stream)
        {
            _image.Save(stream);
            return this;
        }

        /// <inheritdoc/>
        public IImage Save(string path)
        {
            _image.Save(path);
            return this;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
                _image.Dispose();
        }
    }
}
