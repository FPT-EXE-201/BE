using AutoMapper;
using FPT.EXE201.Application.DTOs.Pregnancies;
using FPT.EXE201.Application.DTOs.PrenatalVisits;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.MapperProfiles;

public class PregnancyMappingProfile : Profile
{
    public PregnancyMappingProfile()
    {
        CreateMap<CreatePregnancyDto, Pregnancy>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PregnancyNumber, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.ExpectedDeliveryDate, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentGestationalWeek, opt => opt.Ignore())
            .ForMember(dest => dest.ActualDeliveryDate, opt => opt.Ignore())
            .ForMember(dest => dest.DeliveryMethod, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Conditions, opt => opt.Ignore())
            .ForMember(dest => dest.Visits, opt => opt.Ignore())
            .ForMember(dest => dest.Tests, opt => opt.Ignore());

        CreateMap<CreatePrenatalVisitDto, PrenatalVisit>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PregnancyId, opt => opt.Ignore())
            .ForMember(dest => dest.Pregnancy, opt => opt.Ignore())
            .ForMember(dest => dest.Tests, opt => opt.Ignore());
    }
}
