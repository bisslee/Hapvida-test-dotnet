using MediatR;

namespace Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;

public class GetWeatherRequest : IRequest<GetWeatherResponse>
{
    public int Days { get; set; } = 3;
}

