using QuestPDF.Infrastructure;

namespace QuestPDFGenerator
{
    public interface IDocument
    {
        DocumentMetadata GetMetadata();
        DocumentSettings GetSettings();
        void Compose(IDocumentContainer container);
    }
}
