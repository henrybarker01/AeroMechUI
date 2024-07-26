using QuestPDF.Infrastructure;

namespace AeroMech.API.Reports
{
	public interface IDocument
	{
		DocumentMetadata GetMetadata();
		DocumentSettings GetSettings();
		void Compose(IDocumentContainer container);
	}
}
