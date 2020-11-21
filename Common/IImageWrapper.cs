using System;

namespace Common
{
    public interface IImageWrapper
    {
        Span<uint> GetAllRows { get; }
        int Height { get; }
        int Width { get; }

        void CropRightColumns(int columnsToCrop);
        void Dispose();
        Span<uint> GetRow(int rowIndex);
    }
}


