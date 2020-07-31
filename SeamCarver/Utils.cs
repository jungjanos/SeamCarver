using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Common;

namespace SeamCarver
{
    public static class Utils
    {
        public static ImageWrapper LoadImageAsWrappedRgba(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"The speciefied file was not found {path}");

            Image<Rgba32> image = null;

            try
            {
                image = (Image<Rgba32>)Image.Load(path);
            }
            catch (Exception ex)
            {
                image?.Dispose();
                throw ex;
            }

            if (image.Width > 5000 || image.Height > 5000)
                throw new NotSupportedException($"Image is too large {image.Width } x {image.Height } (WxH) currently max 5000x5000px is supported");

            return new ImageWrapper(image);
        }
    }

    public interface IImageWrapper
    {
        Span<uint> GetAllRaws { get; }
        int Height { get; }
        int Width { get; }

        void CropRightColumns(int columnsToCrop);
        void Dispose();
        Span<uint> GetRow(int rowIndex);
    }

    public enum ImageFormat
    {
        bmp = 1,
        jpeg = 2
    }

    /// <summary>
    /// Thin wrapper around SixLabors.ImageSharp.Image<Rgba32>. 
    /// Caller is responsible to not read Spans above image dimensions
    /// </summary>
    public struct ImageWrapper : IDisposable, IImageWrapper
    {
        internal ImageWrapper(Image<Rgba32> image)
        {
            Common.Utils.GuardNotNull(image);
            _image = image;
        }
        private Image<Rgba32> _image;
        public Span<uint> GetRow(int rowIndex) => MemoryMarshal.Cast<Rgba32, uint>(_image.GetPixelRowSpan(rowIndex));
        public Span<uint> GetAllRaws
        {
            get
            {
                if (!_image.TryGetSinglePixelSpan(out Span<Rgba32> span))
                    return Span<uint>.Empty;

                return MemoryMarshal.Cast<Rgba32, uint>(span);
            }
        }

        public void Dispose() => _image.Dispose();
        public int Width => _image.Width;
        public int Height => _image.Height;

        public void Save(FileStream fs, ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.bmp: { _image.SaveAsBmp(fs); break; }
                case ImageFormat.jpeg: { _image.SaveAsJpeg(fs); break; }
                default: throw new InvalidEnumArgumentException($"format not supported {typeof(ImageFormat)}: {format}");
            }            
        }

        public void CropRightColumns(int columnsToCrop)
        {
            if (columnsToCrop < 1 || columnsToCrop > Width)
                throw new ArgumentOutOfRangeException(nameof(columnsToCrop), $"Tried to crop {columnsToCrop} columns, allowed: {1} - {Width}");

            int w = Width;
            int h = Height;

            _image.Mutate(c => { c.Crop(w - columnsToCrop, h); });
        }

    }
}


