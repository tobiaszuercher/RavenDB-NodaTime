// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using System.IO;
using NodaTime;
using Raven.Imports.Newtonsoft.Json;

namespace Raven.Imports.NodaTime.Serialization.JsonNet
{
    /// <summary>
    /// Json.NET converter for <see cref="Duration"/>.
    /// </summary>
    internal sealed class NodaDurationConverter : NodaConverterBase<Duration>
    {
        /// <summary>
        /// Reads a string from the reader, and converts it to a duration.
        /// </summary>
        /// <param name="reader">The JSON reader to fetch data from.</param>
        /// <param name="serializer">The serializer for embedded serialization.</param>
        /// <returns>The <see cref="DateTimeZone"/> identified in the JSON, or null.</returns>
        protected override Duration ReadJsonImpl(JsonReader reader, JsonSerializer serializer)
        {
            var durationText = (string)reader.Value;

            var parts = durationText.Split(':');
            if (parts.Length < 2)
            {
                throw new InvalidDataException("A Duration must have at least hours and minutes in hh:mm format.");
            }

            if (parts.Length > 3)
            {
                throw new InvalidDataException("Too many components provided for a duration.  Should be in hh:mm:ss.fffffff format.");
            }

            var duration = Duration.Zero;

            long hours;
            if (!long.TryParse(parts[0], out hours))
            {
                throw new InvalidDataException("Invalid hours component of duration.");
            }

            duration += Duration.FromHours(hours);

            long minutes;
            if (!long.TryParse(parts[1], out minutes))
            {
                throw new InvalidDataException("Invalid minutes component of duration.");
            }

            duration += Duration.FromMinutes(minutes);

            decimal seconds;
            if (!decimal.TryParse(parts[2], out seconds))
            {
                throw new InvalidDataException("Invalid seconds component of duration.");
            }

            // if we convert to ticks, we get all fractional parts of the seconds component at once.
            var ticks = Convert.ToInt64(seconds * 1000 * 10000);

            duration += Duration.FromTicks(ticks);

            return duration;
        }

        /// <summary>
        /// Converts the given duration to JSON.
        /// </summary>
        /// <param name="writer">The writer to write to</param>
        /// <param name="value">The value to convert</param>
        /// <param name="serializer">Unused by this serializer</param>
        protected override void WriteJsonImpl(JsonWriter writer, Duration value, JsonSerializer serializer)
        {
            var bclTicks = value.BclCompatibleTicks;
            var hours = bclTicks / NodaConstants.TicksPerHour;
            var minutes = (bclTicks % NodaConstants.TicksPerHour) / NodaConstants.TicksPerMinute;
            var seconds = (bclTicks % NodaConstants.TicksPerMinute) / NodaConstants.TicksPerSecond;
            var milliseconds = (bclTicks % NodaConstants.TicksPerSecond) / NodaConstants.TicksPerMillisecond;
            var ticks = bclTicks % NodaConstants.TicksPerMillisecond;

            var durationText = string.Format("{0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);

            if (milliseconds > 0 || ticks > 0)
            {
                durationText += string.Format(".{0:D3}", milliseconds);

                if (ticks > 0)
                {
                    durationText += string.Format("{0:D4}", ticks);
                }
            }

            writer.WriteValue(durationText);
        }
    }
}
