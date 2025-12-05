using System.Numerics;
using System.Text;

namespace Glimpse.API;

public struct SemVer : IEquatable<SemVer>, IComparable<SemVer>
{
    public int Major;

    public int? Minor;

    public int? Patch;

    public int? Build;

    public string? Suffix;

    public string? Metadata;

    public int Version => Major;
    
    public bool IsPrerelease => Suffix != null;

    public SemVer? SuffixVersion
    {
        get
        {
            if (Suffix == null)
                return null;

            int dotIndex = Suffix.IndexOf('.');
            if (dotIndex < 0)
                return new SemVer(0);

            try
            {
                return new SemVer(Suffix[(dotIndex + 1)..]);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public SemVer(int version)
    {
        Major = version;
    }
    
    public SemVer(int major, int minor = 0, int patch = 0, int? build = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Build = build;
    }

    public SemVer(string versionString)
    {
        int suffixIndex = versionString.IndexOf('-');
        int metadataIndex = versionString.IndexOf('+');

        int currentPosition = 0;
        int dot = 0;
        bool active = true;

        while (active)
        {
            int nextPosition = versionString.IndexOf('.', currentPosition);
            int readTo = nextPosition;
            if (readTo < 0 || (suffixIndex > 0 && readTo > suffixIndex) || (metadataIndex > 0 && readTo > metadataIndex))
            {
                active = false;
                if (suffixIndex < 0)
                    readTo = metadataIndex < 0 ? versionString.Length : metadataIndex;
                else
                    readTo = suffixIndex;
            }

            int version = int.Parse(versionString[currentPosition..readTo]);

            switch (dot++)
            {
                case 0:
                    Major = version;
                    break;
                case 1:
                    Minor = version;
                    break;
                case 2:
                    Patch = version;
                    break;
                case 3:
                    Build = version;
                    break;
                default:
                    active = false;
                    break;
            }

            currentPosition = nextPosition + 1;
        }

        if (suffixIndex > 0)
        {
            int readTo = metadataIndex < 0 ? versionString.Length : metadataIndex;
            Suffix = versionString[(suffixIndex + 1)..readTo];
        }

        if (metadataIndex > 0)
            Metadata = versionString[(metadataIndex + 1)..];
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(Major);
        
        if (Minor != null)
        {
            builder.Append('.');
            builder.Append(Minor);
        }

        if (Patch != null)
        {
            
            builder.Append('.');
            builder.Append(Patch);
        }

        if (Build != null)
        {
            builder.Append('.');
            builder.Append(Build);
        }

        if (Suffix != null)
        {
            builder.Append('-');
            builder.Append(Suffix);
        }

        /*if (Metadata != null)
        {
            builder.Append('+');
            builder.Append(Metadata);
        }*/

        return builder.ToString();
    }

    public int CompareTo(SemVer other)
    {
        int majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
            return majorComparison;

        if (Minor is { } minor)
        {
            int minorComparison = minor.CompareTo(other.Minor);
            if (minorComparison != 0)
                return minorComparison;
        }

        if (Patch is { } patch)
        {
            int patchComparison = patch.CompareTo(other.Patch);
            if (patchComparison != 0)
                return patchComparison;
        }

        if (Build is { } build)
        {
            int buildComparison = build.CompareTo(other.Build);
            if (buildComparison != 0)
                return buildComparison;
        }

        // Versions without a suffix are greater than versions with a suffix
        if (Suffix != null && other.Suffix == null)
            return -1;
        if (Suffix == null && other.Suffix != null)
            return 1;
        
        return string.CompareOrdinal(Suffix, other.Suffix);
    }
    
    public bool Equals(SemVer other)
    {
        return Major == other.Major && Minor == other.Minor && Patch == other.Patch && Build == other.Build &&
               Suffix == other.Suffix;
    }
    
    public override bool Equals(object? obj)
    {
        return obj is SemVer other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, Build, Suffix, Metadata);
    }

    public static bool operator >(SemVer left, SemVer right)
        => left.CompareTo(right) > 0;

    public static bool operator <(SemVer left, SemVer right)
        => left.CompareTo(right) < 0;

    public static bool operator >=(SemVer left, SemVer right)
        => left.CompareTo(right) >= 0;

    public static bool operator <=(SemVer left, SemVer right)
        => left.CompareTo(right) <= 0;

    public static bool operator ==(SemVer left, SemVer right)
        => left.Equals(right);

    public static bool operator !=(SemVer left, SemVer right)
        => !left.Equals(right);
}