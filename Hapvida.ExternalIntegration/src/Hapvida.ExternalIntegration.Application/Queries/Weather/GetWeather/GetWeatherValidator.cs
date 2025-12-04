using FluentValidation;
using Hapvida.ExternalIntegration.Application.Helpers;
using Hapvida.ExternalIntegration.Domain.Resources;

namespace Hapvida.ExternalIntegration.Application.Queries.Weather.GetWeather;

public class GetWeatherValidator : AbstractValidator<GetWeatherRequest>
{
    public GetWeatherValidator()
    {
        RuleFor(x => x.Days)
            .InclusiveBetween(1, 7)
            .WithMessage(_ => ResourceHelper.GetResource("WEATHER_DAYS_INVALID", "O número de dias deve estar entre 1 e 7."));
    }
}

