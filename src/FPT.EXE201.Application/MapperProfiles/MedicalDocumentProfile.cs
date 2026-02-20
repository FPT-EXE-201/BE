using AutoMapper;
using FPT.EXE201.Application.DTOs.MedicalDocuments;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.MapperProfiles;

public class MedicalDocumentProfile : Profile
{
    public MedicalDocumentProfile()
    {
        // MedicalDocument → MedicalDocumentDto
        CreateMap<MedicalDocument, MedicalDocumentDto>()
            .ForMember(dest => dest.DocumentTypeDisplayName,
                opt => opt.MapFrom(src => src.DocumentType != null
                    ? src.DocumentType.Translations.FirstOrDefault()!.DisplayName
                    : null))
            .ForMember(dest => dest.Files,
                opt => opt.MapFrom(src => src.Files.OrderBy(f => f.SortOrder)))
            .ForMember(dest => dest.TotalFileSizeBytes,
                opt => opt.MapFrom(src => src.Files.Sum(f => f.StorageFile.FileSizeBytes)));

        // DocumentFile → DocumentFileDto
        CreateMap<DocumentFile, DocumentFileDto>()
            .ForMember(dest => dest.OriginalFileName,
                opt => opt.MapFrom(src => src.StorageFile.OriginalFileName))
            .ForMember(dest => dest.MimeType,
                opt => opt.MapFrom(src => src.StorageFile.MimeType))
            .ForMember(dest => dest.FileSizeBytes,
                opt => opt.MapFrom(src => src.StorageFile.FileSizeBytes))
            .ForMember(dest => dest.FileUrl,
                opt => opt.MapFrom(src => src.StorageFile.PublicUrl));

        // UpdateMedicalDocumentDto → MedicalDocument (partial update)
        CreateMap<UpdateMedicalDocumentDto, MedicalDocument>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PregnancyId, opt => opt.Ignore())
            .ForMember(dest => dest.CapturedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Source, opt => opt.Ignore())
            .ForMember(dest => dest.IsFavorite, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Pregnancy, opt => opt.Ignore())
            .ForMember(dest => dest.Visit, opt => opt.Ignore())
            .ForMember(dest => dest.DocumentType, opt => opt.Ignore())
            .ForMember(dest => dest.Files, opt => opt.Ignore())
            .ForMember(dest => dest.OcrResults, opt => opt.Ignore());

        // OcrResult → OcrResultDto
        CreateMap<OcrResult, OcrResultDto>();
    }
}
