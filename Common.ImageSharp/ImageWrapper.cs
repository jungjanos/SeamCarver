﻿using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Common.ImageSharp
{
    /// <summary>
    /// Thin wrapper around SixLabors.ImageSharp.Image<Rgba32>. 
    /// Caller is responsible to not read Spans above image dimensions
    /// </summary>
    public struct ImageWrapper : IDisposable, IImageWrapper
    {
        private string _path;
        private Image<Rgba32> _image => _proxy.Value;
        private Lazy<Image<Rgba32>> _proxy;
        public int Width => _proxy.IsValueCreated ? _image.Width : GetDimensions().width;
        public int Height => _proxy.IsValueCreated ? _image.Height : GetDimensions().height;


        public ImageWrapper(Lazy<Image<Rgba32>> proxy, string path)
        {
            Utils.GuardNotNull(proxy);
            _proxy = proxy;
            _path = path;
        }

        public static ImageWrapper Create(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"The speciefied file was not found {path}");

            var proxy = new Lazy<Image<Rgba32>>(() =>
           {
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

               return image;
           }, false);
            return new ImageWrapper(proxy, path);
        }

        // TODO : verify that this call is fast!
        private (int width, int height) GetDimensions()
        {
            var timer = Stopwatch.StartNew();

            var info = Image.Identify(_path);
            timer.Stop();
            Debug.WriteLine($"{nameof(GetDimensions)} took {timer.ElapsedMilliseconds}ms");
            
            return (info.Width, info.Height);            
        }

        public Span<uint> GetRow(int rowIndex) => MemoryMarshal.Cast<Rgba32, uint>(_image.GetPixelRowSpan(rowIndex));
        public Span<uint> GetAllRows
        {
            get
            {
                if (!_image.TryGetSinglePixelSpan(out Span<Rgba32> span))
                    return Span<uint>.Empty;

                return MemoryMarshal.Cast<Rgba32, uint>(span);
            }
        }

        public void Dispose() => _image.Dispose();

        public void SaveTo(FileStream fs, ImageFormat format)
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
