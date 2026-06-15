// Copyright 2026 Entex Interactive

namespace Parallel.Core.Models
{
    public class PullRecord
    {
        /// <summary>
        /// The machine to use this record.
        /// </summary>
        public string Machine { get; } = Environment.MachineName;
        
        /// <summary>
        /// The source path to pull.
        /// </summary>
        public string Source { get; }
        
        /// <summary>
        /// The destination to remap files to.
        /// </summary>
        public string Destination { get; set; }
        
        /// <summary>
        /// Creates a new instance of the <see cref="PullRecord"/> class.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <exception cref="NotImplementedException"></exception>
        public PullRecord(string source, string? destination)
        {
            Source = source;
            Destination = destination ?? source;
        }
        
        public override bool Equals(object? obj) => obj is PullRecord or && Machine == or.Machine && Source == or.Source;
        public override int GetHashCode() => HashCode.Combine(Machine, Source);
    }
}