using FluentValidation;

namespace ahis.template.application.Features.CountryFeatures.Command
{
    public class AddCountryCommandValidator : AbstractValidator<AddCountryCommand>
    {
        public AddCountryCommandValidator()
        {
            RuleFor(x => x.CountryFullname)
                .NotEmpty().WithMessage("CountryFullname is required.")
                .Length(2, 200).WithMessage("CountryFullname must be between 2 and 200 characters.");

            RuleFor(x => x.CountryShortname)
                .NotEmpty().WithMessage("CountryShortname is required.")
                .Length(1, 100).WithMessage("CountryShortname must be between 1 and 100 characters.");

            RuleFor(x => x.CountryCode2)
                .NotEmpty().WithMessage("CountryCode2 is required.")
                .Length(2).WithMessage("CountryCode2 must be 2 characters.")
                .Matches("^[A-Za-z]{2}$").WithMessage("CountryCode2 must be alphabetic.");

            RuleFor(x => x.CountryCode3)
                .NotEmpty().WithMessage("CountryCode3 is required.")
                .Length(2, 3).WithMessage("CountryCode3 must be 2 or 3 characters.")
                .Matches("^[A-Za-z]{2,3}$").WithMessage("CountryCode3 must be alphabetic.");

            RuleFor(x => x.CountryDescription)
                .MaximumLength(1000).WithMessage("CountryDescription cannot exceed 1000 characters.");
        }
    }
}
