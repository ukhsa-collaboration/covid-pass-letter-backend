namespace CovidLetter.Backend.Common.Application.Profiles;

using System.Globalization;
using AutoMapper;

public class LetterProfile : Profile
{
    public LetterProfile()
    {
        this.SourceMemberNamingConvention = new LowerUnderscoreNamingConvention();
        this.DestinationMemberNamingConvention = new PascalCaseNamingConvention();

        this.CreateMap<LetterRequest, Letter>()
            .ForMember(
                src => src.Address_Line_1,
                opt => opt.MapFrom(src => new[] { src.AddressLine1 }))
            .ForMember(
                src => src.Address_Line_2,
                opt => opt.MapFrom(src => new[] { src.AddressLine2 }))
            .ForMember(
                src => src.Address_Line_3,
                opt => opt.MapFrom(src => new[] { src.AddressLine3 }))
            .ForMember(
                src => src.Address_Line_4,
                opt => opt.MapFrom(src => new[] { src.AddressLine4 }))
            .ForMember(
                src => src.Accessibility_Needs,
                opt => opt.MapFrom(src => src.HasAccessibilityNeeds ? src.AccessibilityNeeds : string.Empty))
            .ForMember(
                src => src.Alternate_Language,
                opt => opt.MapFrom(src => src.HasAlternateLanguage ? src.AlternateLanguage : string.Empty))
            .ForMember(
                src => src.Date_of_birth,
                opt => opt.MapFrom(dest =>
                    DateTime.ParseExact(dest.DateOfBirth, "yyyy-MM-dd", CultureInfo.InvariantCulture)))
            .ForMember(src => src.Forename, opt => opt.MapFrom(dest => dest.FirstName))
            .ForMember(src => src.NHS_Number, opt => opt.MapFrom(dest => dest.NhsNumber))
            .ForMember(src => src.Surname, opt => opt.MapFrom(dest => dest.LastName))
            .ForMember(src => src.UID, opt => opt.MapFrom(dest => dest.CorrelationId))
            .ForAllMembers(opt => opt.Condition((_, _, srcMember) => srcMember != null));
    }
}
