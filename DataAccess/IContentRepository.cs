using System.Collections.Generic;
using FreeCMS.Entities;

namespace FreeCMS
{
    public interface IContentRepository
    {
        bool AddContent(ContentUnitDTO input);
        bool UpdateContent(ContentUnitDTO input);
        bool RemoveContent(int ContentId);
        List<ContentUnitDTO> GetContent(string contentName);
    }
}