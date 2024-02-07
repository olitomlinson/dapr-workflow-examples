using System.Collections.Concurrent;
using System.Diagnostics.Contracts;

public class TimingMetadata
{
    public TimingSplits Splits { get; set; }

    public List<TimingItem> SplitsGrouped { get; set; }

    public TimingMetadata()
    {
        Splits = new TimingSplits();
    }
}

public class TimingSplits
{
    public TimingSplits()
    {
        Durations = new List<TimingItem>();
    }
    public List<TimingItem> Durations { get; set; }

    public long TotalDurationOfStartOperationsSum { 
        get {
            return Durations.Sum(x => x.Duration);
        }
    }

    public double TotalDurationOfStartOperationsStdDev { 
        get {
            return StdDev(Durations.Select(x => (int)x.Duration), false);
        }
    }

    public double DurationAverage { 
        get {
            return Durations.Average(x => x.Duration);
        }
    }

    public TimeSpan TotalDurationOfStartOperationsWallClock { 
        get {

            var orderedDurations = Durations.OrderBy(x => x.Timestamp);
            return orderedDurations.Last().Timestamp.Subtract(orderedDurations.First().Timestamp);
        } 
    }

    // Return the standard deviation of an array of Doubles.
    //
    // If the second argument is True, evaluate as a sample.
    // If the second argument is False, evaluate as a population.
    private double StdDev(IEnumerable<int> values,
        bool as_sample)
    {
        // Get the mean.
        double mean = values.Sum() / values.Count();

        // Get the sum of the squares of the differences
        // between the values and the mean.
        var squares_query =
            from int value in values
            select (value - mean) * (value - mean);
        double sum_of_squares = squares_query.Sum();

        if (as_sample)
        {
            return Math.Sqrt(sum_of_squares / (values.Count() - 1));
        }
        else
        {
            return Math.Sqrt(sum_of_squares / values.Count());
        }
    }

}

public class TimingItem
{
    public DateTime Timestamp { get; set; } 
    public int Duration { get; set; }
}