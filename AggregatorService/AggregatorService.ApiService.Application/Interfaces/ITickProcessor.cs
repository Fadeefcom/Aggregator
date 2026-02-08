using AggregatorService.ApiService.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AggregatorService.ApiService.Application.Interfaces;

public interface ITickProcessor
{
    bool ShouldProcess(Tick tick);
    Tick Normalize(Tick tick);
    bool IsDuplicate(Tick tick);
    IEnumerable<Candle> UpdateMetricsAndAggregate(Tick tick);

    SourceStatus GetSourceStatus(string source);
    IEnumerable<SourceStatus> GetAllSourceStatuses();
}
