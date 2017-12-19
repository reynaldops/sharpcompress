﻿#if RARWIP
#if !Rar2017_64bit
using nint = System.Int32;
using nuint = System.UInt32;
using size_t = System.UInt32;
#else
using nint = System.Int64;
using nuint = System.UInt64;
using size_t = System.UInt64;
#endif
using int64 = System.Int64;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpCompress.Compressors.Rar.UnpackV2017
{
    partial class FragmentedWindow
    {

public FragmentedWindow()
{
  //memset(Mem,0,sizeof(Mem));
  //memset(MemSize,0,sizeof(MemSize));
}


//FragmentedWindow::~FragmentedWindow()
//{
//  Reset();
//}


void Reset()
{
  for (uint I=0;I<Mem.Length;I++)
    if (Mem[I]!=null)
    {
      //free(Mem[I]);
      Mem[I]=null;
    }
}


public void Init(size_t WinSize)
{
  Reset();

  uint BlockNum=0;
  size_t TotalSize=0; // Already allocated.
  while (TotalSize<WinSize && BlockNum<Mem.Length)
  {
    size_t Size=WinSize-TotalSize; // Size needed to allocate.

    // Minimum still acceptable block size. Next allocations cannot be larger
    // than current, so we do not need blocks if they are smaller than
    // "size left / attempts left". Also we do not waste time to blocks
    // smaller than some arbitrary constant.
    size_t MinSize=Math.Max(Size/(size_t)(Mem.Length-BlockNum), 0x400000);

    byte[] NewMem=null;
    while (Size>=MinSize)
    {
      NewMem=new byte[Size];
      if (NewMem!=null)
        break;
      Size-=Size/32;
    }
    if (NewMem==null)
      //throw std::bad_alloc();
      throw new InvalidOperationException();
    
    // Clean the window to generate the same output when unpacking corrupt
    // RAR files, which may access to unused areas of sliding dictionary.
    // sharpcompress: don't need this, freshly allocated above
    //Utility.Memset(NewMem,0,Size);

    Mem[BlockNum]=NewMem;
    TotalSize+=Size;
    MemSize[BlockNum]=TotalSize;
    BlockNum++;
  }
  if (TotalSize<WinSize) // Not found enough free blocks.
    //throw std::bad_alloc();
    throw new InvalidOperationException();
}


byte& operator [](size_t Item)
{
  if (Item<MemSize[0])
    return Mem[0][Item];
  for (uint I=1;I<ASIZE(MemSize);I++)
    if (Item<MemSize[I])
      return Mem[I][Item-MemSize[I-1]];
  return Mem[0][0]; // Must never happen;
}


public void CopyString(uint Length,uint Distance,size_t &UnpPtr,size_t MaxWinMask)
{
  size_t SrcPtr=UnpPtr-Distance;
  while (Length-- > 0)
  {
    (*this)[UnpPtr]=(*this)[SrcPtr++ & MaxWinMask];
    // We need to have masked UnpPtr after quit from loop, so it must not
    // be replaced with '(*this)[UnpPtr++ & MaxWinMask]'
    UnpPtr=(UnpPtr+1) & MaxWinMask;
  }
}


void CopyData(byte *Dest,size_t WinPos,size_t Size)
{
  for (size_t I=0;I<Size;I++)
    Dest[I]=(*this)[WinPos+I];
}


public size_t GetBlockSize(size_t StartPos,size_t RequiredSize)
{
  for (uint I=0;I<ASIZE(MemSize);I++)
    if (StartPos<MemSize[I])
      return Math.Min(MemSize[I]-StartPos,RequiredSize);
  return 0; // Must never be here.
}

    }
}
#endif