using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CreamInstaller.Utility;

/// <summary>
/// Minimal, streaming reader for the import and delay-import tables of a PE image.
/// Only reads the headers, descriptor tables and module names it needs; never loads the whole file.
/// </summary>
internal static class PeImports
{
    private const ushort DosSignature = 0x5A4D; // MZ
    private const uint PeSignature = 0x00004550; // PE\0\0
    private const ushort Pe32Magic = 0x10B;
    private const ushort Pe32PlusMagic = 0x20B;
    private const int ImportDescriptorSize = 20;
    private const int DelayImportDescriptorSize = 32;
    private const int MaxDescriptors = 8192;
    private const int MaxModuleNameLength = 512;

    private static readonly Dictionary<string, (long Ticks, long Length, HashSet<string> Modules)> Cache =
        new(System.StringComparer.OrdinalIgnoreCase);

    private static readonly object CacheLock = new();

    /// <summary>
    /// Returns every module name referenced by the image's import and delay-import tables
    /// (lowercase, including the <c>.dll</c> extension). Returns <c>false</c> for unreadable
    /// or non-PE files; never throws.
    /// </summary>
    internal static bool TryGetImportedModules(string path, out HashSet<string> modules)
    {
        modules = null;
        if (string.IsNullOrWhiteSpace(path))
            return false;

        string fullPath;
        FileInfo info;
        try
        {
            fullPath = Path.GetFullPath(path);
            info = new FileInfo(fullPath);
            if (!info.Exists)
                return false;
        }
        catch
        {
            return false;
        }

        lock (CacheLock)
            if (Cache.TryGetValue(fullPath, out (long Ticks, long Length, HashSet<string> Modules) cached) &&
                cached.Ticks == info.LastWriteTimeUtc.Ticks && cached.Length == info.Length)
            {
                modules = cached.Modules;
                return modules is not null;
            }

        HashSet<string> parsed = null;
        try
        {
            parsed = Parse(fullPath);
        }
        catch (Exception e)
        {
            ProgramData.Log.Warn($"[PeImports] Failed to read imports from \"{fullPath}\": {e.Message}");
        }

        lock (CacheLock)
            Cache[fullPath] = (info.LastWriteTimeUtc.Ticks, info.Length, parsed);

        modules = parsed;
        return modules is not null;
    }

    private static HashSet<string> Parse(string fullPath)
    {
        using FileStream stream = new(fullPath, FileMode.Open, FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        long length = stream.Length;
        if (length < 0x40)
            return null;

        if (!TryReadUInt16(stream, length, 0, out ushort dosSignature) || dosSignature != DosSignature)
            return null;
        if (!TryReadInt32(stream, length, 0x3C, out int peOffset) || peOffset < 0 || peOffset + 0x18 > length)
            return null;
        if (!TryReadUInt32(stream, length, peOffset, out uint peSignature) || peSignature != PeSignature)
            return null;

        long coffOffset = peOffset + 4L;
        if (!TryReadUInt16(stream, length, coffOffset + 2, out ushort sectionCount) ||
            !TryReadUInt16(stream, length, coffOffset + 16, out ushort optionalHeaderSize))
            return null;

        long optionalOffset = coffOffset + 20;
        if (optionalHeaderSize < 2 || optionalOffset + optionalHeaderSize > length)
            return null;
        if (!TryReadUInt16(stream, length, optionalOffset, out ushort magic))
            return null;

        long dataDirectoryOffset;
        ulong imageBase;
        switch (magic)
        {
            case Pe32Magic:
                if (optionalHeaderSize < 96)
                    return null;
                dataDirectoryOffset = optionalOffset + 96;
                if (!TryReadUInt32(stream, length, optionalOffset + 28, out uint imageBase32))
                    return null;
                imageBase = imageBase32;
                break;
            case Pe32PlusMagic:
                if (optionalHeaderSize < 112)
                    return null;
                dataDirectoryOffset = optionalOffset + 112;
                if (!TryReadUInt64(stream, length, optionalOffset + 24, out ulong imageBase64))
                    return null;
                imageBase = imageBase64;
                break;
            default:
                return null;
        }

        if (!TryReadInt32(stream, length, dataDirectoryOffset - 4, out int directoryCount) || directoryCount < 0)
            return null;

        long sectionTableOffset = optionalOffset + optionalHeaderSize;
        if (sectionCount == 0 || sectionTableOffset + sectionCount * 40L > length)
            return null;

        List<Section> sections = new(sectionCount);
        for (int i = 0; i < sectionCount; i++)
        {
            long sectionOffset = sectionTableOffset + i * 40L;
            if (!TryReadUInt32(stream, length, sectionOffset + 8, out uint virtualSize) ||
                !TryReadUInt32(stream, length, sectionOffset + 12, out uint virtualAddress) ||
                !TryReadUInt32(stream, length, sectionOffset + 16, out uint sizeOfRawData) ||
                !TryReadUInt32(stream, length, sectionOffset + 20, out uint pointerToRawData))
                return null;
            sections.Add(new Section(virtualAddress, virtualSize, sizeOfRawData, pointerToRawData));
        }

        HashSet<string> modules = new(System.StringComparer.OrdinalIgnoreCase);

        if (directoryCount > 1 &&
            TryReadUInt32(stream, length, dataDirectoryOffset + 1 * 8, out uint importRva) && importRva != 0)
            ReadImportModules(stream, length, importRva, sections, modules);

        if (directoryCount > 13 &&
            TryReadUInt32(stream, length, dataDirectoryOffset + 13 * 8, out uint delayImportRva) &&
            delayImportRva != 0)
            ReadDelayImportModules(stream, length, delayImportRva, sections, imageBase, modules);

        return modules;
    }

    private static void ReadImportModules(FileStream stream, long length, uint rva, List<Section> sections,
        HashSet<string> modules)
    {
        byte[] descriptor = new byte[ImportDescriptorSize];
        for (int i = 0; i < MaxDescriptors; i++)
        {
            int descriptorOffset = RvaToFileOffset(rva + (uint)(i * ImportDescriptorSize), sections);
            if (descriptorOffset < 0 || !TryRead(stream, length, descriptorOffset, descriptor))
                return;
            uint nameRva = BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(12, sizeof(uint)));
            if (nameRva == 0)
                return;
            AddModuleName(stream, length, nameRva, sections, modules);
        }
    }

    private static void ReadDelayImportModules(FileStream stream, long length, uint rva, List<Section> sections,
        ulong imageBase, HashSet<string> modules)
    {
        byte[] descriptor = new byte[DelayImportDescriptorSize];
        for (int i = 0; i < MaxDescriptors; i++)
        {
            int descriptorOffset = RvaToFileOffset(rva + (uint)(i * DelayImportDescriptorSize), sections);
            if (descriptorOffset < 0 || !TryRead(stream, length, descriptorOffset, descriptor))
                return;
            uint attributes = BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(0, sizeof(uint)));
            uint nameAddress = BinaryPrimitives.ReadUInt32LittleEndian(descriptor.AsSpan(4, sizeof(uint)));
            if (nameAddress == 0)
                return;

            uint nameRva = nameAddress;
            if ((attributes & 1) == 0)
            {
                // Legacy descriptors store a virtual address instead of an RVA.
                if (nameAddress < imageBase || nameAddress - imageBase > uint.MaxValue)
                    continue;
                nameRva = (uint)(nameAddress - imageBase);
            }

            AddModuleName(stream, length, nameRva, sections, modules);
        }
    }

    private static void AddModuleName(FileStream stream, long length, uint nameRva, List<Section> sections,
        HashSet<string> modules)
    {
        int nameOffset = RvaToFileOffset(nameRva, sections);
        if (nameOffset < 0 || nameOffset >= length)
            return;

        int available = (int)Math.Min(MaxModuleNameLength, length - nameOffset);
        byte[] buffer = new byte[available];
        if (!TryRead(stream, length, nameOffset, buffer))
            return;

        int terminator = Array.IndexOf(buffer, (byte)0);
        string name = Encoding.ASCII.GetString(buffer, 0, terminator < 0 ? buffer.Length : terminator).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        try
        {
            name = Path.GetFileName(name);
        }
        catch
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(name))
            modules.Add(name.ToLowerInvariant());
    }

    private static int RvaToFileOffset(uint rva, List<Section> sections)
    {
        foreach (Section section in sections)
        {
            uint span = Math.Max(section.VirtualSize, section.SizeOfRawData);
            if (rva < section.VirtualAddress || rva - section.VirtualAddress >= span)
                continue;

            uint delta = rva - section.VirtualAddress;
            if (delta >= section.SizeOfRawData)
                return -1;

            long offset = section.PointerToRawData + (long)delta;
            return offset is < 0 or > int.MaxValue ? -1 : (int)offset;
        }

        return -1;
    }

    private static bool TryRead(FileStream stream, long length, long offset, byte[] buffer)
    {
        if (offset < 0 || offset + buffer.Length > length)
            return false;

        stream.Position = offset;
        int total = 0;
        while (total < buffer.Length)
        {
            int read = stream.Read(buffer, total, buffer.Length - total);
            if (read <= 0)
                return false;
            total += read;
        }

        return true;
    }

    private static bool TryReadUInt16(FileStream stream, long length, long offset, out ushort value)
    {
        value = 0;
        byte[] buffer = new byte[sizeof(ushort)];
        if (!TryRead(stream, length, offset, buffer))
            return false;
        value = BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        return true;
    }

    private static bool TryReadUInt32(FileStream stream, long length, long offset, out uint value)
    {
        value = 0;
        byte[] buffer = new byte[sizeof(uint)];
        if (!TryRead(stream, length, offset, buffer))
            return false;
        value = BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        return true;
    }

    private static bool TryReadUInt64(FileStream stream, long length, long offset, out ulong value)
    {
        value = 0;
        byte[] buffer = new byte[sizeof(ulong)];
        if (!TryRead(stream, length, offset, buffer))
            return false;
        value = BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        return true;
    }

    private static bool TryReadInt32(FileStream stream, long length, long offset, out int value)
    {
        value = 0;
        byte[] buffer = new byte[sizeof(int)];
        if (!TryRead(stream, length, offset, buffer))
            return false;
        value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        return true;
    }

    private readonly record struct Section(uint VirtualAddress, uint VirtualSize, uint SizeOfRawData,
        uint PointerToRawData);
}
