using QuestPDF.Infrastructure;

namespace AeroMech.UI.Web.Reports
{
    public interface IDocument
    {
        DocumentMetadata GetMetadata();
        DocumentSettings GetSettings();
        void Compose(IDocumentContainer container);
    }
}
